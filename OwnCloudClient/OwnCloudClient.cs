using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace OwnCloudClient
{
	public static class OwnCloudClient
	{
		private static string phpId = null;

		private static string GetLocalFilePathFromCloudFileNamePlusDate(string cloudFileNamePlusDate)
		{
			//!FileNameProcessing
			return Settings.WatchDir + cloudFileNamePlusDate.Substring(0, cloudFileNamePlusDate.Length - (4 + 13));
		}

		private static DateTime GetDateTimeFromCloudFilePlusDate(string cloudFileNamePlusDate)
		{
			//!FileNameProcessing
			string sUnixTime = cloudFileNamePlusDate.Substring(cloudFileNamePlusDate.Length - 12, 12);
			return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToDouble(sUnixTime));
		}

		public static string GetUnixTimeStamp(DateTime modifiedDate)
		{
			TimeSpan ts = (modifiedDate - new DateTime(1970, 1, 1, 0, 0, 0));
			return string.Format("{0:000000000000}", ts.TotalSeconds); //12 zeros
		}

		public static bool Login(string userName, string password)
		{
			phpId = GetPhpId();
			bool success = false;

			StringBuilder sb = new StringBuilder();

			sb.AppendLine("-----------------------------7db2172440460");
			sb.AppendLine("Content-Disposition: form-data; name=\"login\"");
			sb.AppendLine("");
			sb.AppendFormat("{0}\r\n", userName);
			sb.AppendLine("-----------------------------7db2172440460");
			sb.AppendLine("Content-Disposition: form-data; name=\"password\"");
			sb.AppendLine("");
			sb.AppendFormat("{0}\r\n", password);
			sb.AppendLine("-----------------------------7db2172440460");
			sb.AppendLine("Content-Disposition: form-data; name=\"loginbutton\"");
			sb.AppendLine("");
			sb.AppendLine("login");
			sb.AppendLine("-----------------------------7db2172440460--");

			WebRequest request = WebRequest.Create(Settings.OwnCloudUrl);
			((HttpWebRequest)request).AllowAutoRedirect = false;

			//http://www.velocityreviews.com/forums/t302174-why-do-i-get-the-server-committed-a-protocol-violation.html
			((HttpWebRequest)request).KeepAlive = false;

			request.Method = "POST";
			request.ContentType = "multipart/form-data; boundary=---------------------------7db2172440460";
			request.Headers.Add("Pragma: no-cache");
			request.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));

			using (Stream dataStream = request.GetRequestStream())
			{
				byte[] datums = Encoding.ASCII.GetBytes(sb.ToString());
				dataStream.Write(datums, 0, datums.Length);

				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				if ((int)response.StatusCode == 302)
				{
					using (Stream responseStream = response.GetResponseStream())
					{
						using (StreamReader reader = new StreamReader(responseStream))
						{
							string responseFromServer = reader.ReadToEnd();
							success = responseFromServer == "\n\n12"; //success message
							reader.Close();
						}
						responseStream.Close();
					}
					response.Close();
				}
				dataStream.Close();
			}
			return success;
		}

		private static string GetPhpId()
		{
			string id = string.Empty;
			using (WebClient wc = new WebClient())
			{
				wc.OpenRead(Settings.OwnCloudUrl);

				foreach (string s in wc.ResponseHeaders.AllKeys)
				{
					if (s == "Set-Cookie")
					{
						//"PHPSESSID=84652f6a9b66943792131acacdf571a8; path=/; HttpOnly"
						string input = wc.ResponseHeaders["Set-Cookie"];
						Match match = Regex.Match(input, @"PHPSESSID\=([^;]+)");

						if (match != null && match.Captures.Count == 1)
							id = match.Groups[1].Value;

						break;
					}
				}
			}
			if (string.IsNullOrEmpty(id))
				throw new Exception("PHPSESSID could not be found");

			return id;
		}

		public static void Download(string cloudFileNamePlusDate)
		{
			try
			{
				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Pragma: no-cache");
					wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
					NLogger.Current.Info(string.Format("Downloading {0}", cloudFileNamePlusDate));
					Uri uri = new Uri(string.Concat(Settings.OwnCloudUrl, "files/api.php?action=get&dir=&file=", cloudFileNamePlusDate)); //cbTODO: urlencode filename?
					wc.DownloadFile(uri, Settings.WatchDir + cloudFileNamePlusDate);
				}

				byte[] decryptedContents = Encryption.DecryptFile(Settings.WatchDir + cloudFileNamePlusDate);
				System.IO.File.Delete(Settings.WatchDir + cloudFileNamePlusDate);

				if (cloudFileNamePlusDate.Contains('~'))
				{
					cloudFileNamePlusDate = cloudFileNamePlusDate.Replace('~', '\\');
					string fileDirectory = cloudFileNamePlusDate.Substring(0, cloudFileNamePlusDate.LastIndexOf('\\'));

					string currentPath = Settings.WatchDir + fileDirectory; ;
					if (!System.IO.Directory.Exists(currentPath))
						System.IO.Directory.CreateDirectory(currentPath);
				}

				string localFileName = GetLocalFilePathFromCloudFileNamePlusDate(cloudFileNamePlusDate);
				System.IO.File.WriteAllBytes(localFileName, decryptedContents);

				DateTime modified = GetDateTimeFromCloudFilePlusDate(cloudFileNamePlusDate);
				File.SetLastWriteTime(localFileName, modified);

				NLogger.Current.Info("Download finished.");
			}
			catch (Exception ex)
			{
				NLogger.Current.ErrorException(string.Format("Problem downloading {0}.", cloudFileNamePlusDate), ex);
			}
		}

		public static List<FileInfoX> GetFileList()
		{
			List<FileInfoX> files = new List<FileInfoX>();
			string json = string.Empty;
			using (WebClient wc = new WebClient())
			{
				wc.Headers.Add("Pragma: no-cache");
				wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
				json = wc.DownloadString(string.Concat(Settings.OwnCloudUrl, "files/api.php?action=getfiles&dir="));
			}

			JObject o = JObject.Parse(json);
			JToken jtk = o.Root;

			foreach (var x in jtk.Values())
			{
				if (x != null && x.SelectToken("name") != null)
				{
					//!FileNameProcessing
					string cloudNamePlusDate = x.SelectToken("name").ToString().Trim(new char[] { '\"' });
					string cloudName = cloudNamePlusDate.Substring(0, cloudNamePlusDate.Length - 13); //13 for '.' + unix date

					//!FileNameProcessing
					string sUnixTime = cloudNamePlusDate.Substring(cloudNamePlusDate.Length - 12, 12);
					DateTime modified = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToDouble(sUnixTime));

					//!FileNameProcessing
					string fileName = cloudName.Replace('~', '\\');
					fileName = fileName.Substring(0, fileName.Length - 4);

					if (!string.IsNullOrEmpty(cloudNamePlusDate))
						files.Add(new FileInfoX() { CloudName = cloudName, LastModified = modified, CloudNamePlusDate = cloudNamePlusDate, FileName = fileName });
				}
			}

			return files;
		}

		public static void PrintFileList()
		{
			foreach (var s in GetFileList())
				NLogger.Current.Info(s.CloudName);
		}

		public static UploadFileStatus UploadFile(string localFullPath)
		{
			UploadFileStatus status = UploadFileStatus.Success;

			FileInfo fi = new FileInfo(localFullPath);
			decimal kbSize = Math.Round(Convert.ToDecimal(fi.Length) / Convert.ToDecimal(Math.Pow(2, 10)), 2);
			NLogger.Current.Info(string.Format("Uploading {0} [{1} KiB]", localFullPath.Replace(Settings.WatchDir, ""), kbSize));

			if (!System.IO.File.Exists(localFullPath))
			{
				NLogger.Current.Warn(string.Format("{0} doesn't exist", localFullPath));
				status = UploadFileStatus.NoFileFound;
			}

			if (kbSize > 10000)
			{
				NLogger.Current.Warn(string.Format("{0} is too large", localFullPath));
				status = UploadFileStatus.FileTooLarge;
			}

			if (status == UploadFileStatus.Success)
			{
				//TODO: check last modified date of cloud file?
				byte[] encrypted = Encryption.EncryptFile(localFullPath);

				//!FileNameProcessing
				string tmpFileName = localFullPath.Replace(Settings.WatchDir, "").Replace('\\', '~') +
							".enc" +
							"." + GetUnixTimeStamp(fi.LastWriteTime);

				System.IO.File.WriteAllBytes(Settings.WatchDir + tmpFileName, encrypted);

				using (MyWebClient wc = new MyWebClient(200000))
				{
					wc.Headers.Add("Pragma: no-cache");
					wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));

					byte[] response = wc.UploadFile(new Uri(Settings.OwnCloudUrl + "files/upload.php?dir="), Settings.WatchDir + tmpFileName);
					status = Encoding.ASCII.GetString(response) == "\n\ntrue" ? UploadFileStatus.Success : UploadFileStatus.UnknownError;
				}
				System.IO.File.Delete(Settings.WatchDir + tmpFileName);
			}

			if (status == UploadFileStatus.Success)
				NLogger.Current.Info("Upload Successful.");
			else
				NLogger.Current.Warn("Upload Unsuccessful.");
			return status;
		}

		public static bool DeleteFile(string cloudFileName)
		{
			bool success = false;
			using (WebClient wc = new WebClient())
			{
				NLogger.Current.Info(string.Format("Deleting {0}", cloudFileName));
				string data3 = string.Format("action=delete&dir=&file={0}", cloudFileName);
				wc.Headers.Add("Pragma: no-cache");
				wc.Headers.Add("Content-Type: application/x-www-form-urlencoded");
				wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
				wc.Headers.Add(string.Format("Referer: {0}", Settings.OwnCloudUrl));
				string response = wc.UploadString(Settings.OwnCloudUrl + "files/api.php", "POST", data3);
				success = response == "true";
			}
			if (success)
				NLogger.Current.Info("Delete Successful");
			else
				NLogger.Current.Warn("Delete Unsuccessful");

			return success;
		}

		public static bool ShouldDownload(FileInfoX f)
		{
			string fname = Settings.WatchDir + f.FileName;
			bool shouldDownload = false;

			if (System.IO.File.Exists(fname))
			{
				System.IO.FileInfo info = new FileInfo(fname);
				shouldDownload = info.LastAccessTime < f.LastModified;
			}
			else
			{
				shouldDownload = true;
			}
			return shouldDownload;
		}

		public static void DownloadAll()
		{
			foreach (var f in GetFileList())
			{
				if (ShouldDownload(f))
					OwnCloudClient.Download(f.CloudNamePlusDate);
			}
		}

		public static void DownloadAll(string startsWith)
		{
			var files = GetFileList();
			foreach (var f in files.Where(x => x.CloudName.StartsWith(startsWith)))
			{
				if (ShouldDownload(f))
					Download(f.CloudNamePlusDate);
			}
		}

	}
}
