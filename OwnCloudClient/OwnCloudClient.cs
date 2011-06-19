using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace DirectorySync
{
	public static class OwnCloudClient
	{
		private static string phpId = null;
		public static readonly string DATA_PATH = Environment.CurrentDirectory + "\\data\\";
		private static readonly string DATA_PREFIX = @"data\";
		private static readonly string OWNCLOUD_URL = "";

		private static string GetLocalFilePathFromCloudFileNamePlusDate(string cloudFileNamePlusDate)
		{
			//!FileNameProcessing
			return DATA_PREFIX + cloudFileNamePlusDate.Substring(0, cloudFileNamePlusDate.Length - (4 + 13));
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

			//wc.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
			//wc.Headers.Add(string.Format("Referer: {0}", OWNCLOUD_URL));
			//wc.Headers.Add("Accept-Language: en-us");
			//wc.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0; WOW64; Trident/5.0)");
			//wc.Headers.Add("Content-Type: multipart/form-data; boundary=---------------------------7db2172440460");
			//wc.Headers.Add("Host: sideproj.dot5hosting.com");
			//wc.Headers.Add("Pragma: no-cache");
			//wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
			//string response = wc.UploadString(OWNCLOUD_URL, "POST", sb.ToString());

			WebRequest request = WebRequest.Create(OWNCLOUD_URL);
			((HttpWebRequest)request).AllowAutoRedirect = false;

			//http://www.velocityreviews.com/forums/t302174-why-do-i-get-the-server-committed-a-protocol-violation.html
			((HttpWebRequest)request).KeepAlive = false;

			request.Method = "POST";
			request.ContentType = "multipart/form-data; boundary=---------------------------7db2172440460";
			request.Headers.Add("Pragma: no-cache");
			request.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
			Stream dataStream = request.GetRequestStream();
			byte[] datums = Encoding.ASCII.GetBytes(sb.ToString());
			dataStream.Write(datums, 0, datums.Length);
				
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			success = (int)response.StatusCode == 302;

			//Stream responseStream = response.GetResponseStream();
			//StreamReader reader = new StreamReader(responseStream);
			//string responseFromServer = reader.ReadToEnd();
			//reader.Close();
			dataStream.Close();
			response.Close();

			return success;
			
		}

		private static string GetPhpId()
		{
			string id = string.Empty;
			using (WebClient wc = new WebClient())
			{
				wc.OpenRead(OWNCLOUD_URL);

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
				//TODO: check last modified date of cloud file compared to local don't download if local is newer
				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Pragma: no-cache");
					wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
					NLogger.Current.Info(string.Format("Downloading {0}", cloudFileNamePlusDate));
					Uri uri = new Uri(string.Concat(OWNCLOUD_URL, "files/api.php?action=get&dir=&file=", cloudFileNamePlusDate));
					wc.DownloadFile(uri, DATA_PREFIX + cloudFileNamePlusDate);
				}

				byte[] decryptedContents = Encryption.DecryptFile(DATA_PREFIX + cloudFileNamePlusDate);
				System.IO.File.Delete(DATA_PREFIX + cloudFileNamePlusDate);

				if (cloudFileNamePlusDate.Contains('~'))
				{
					cloudFileNamePlusDate = cloudFileNamePlusDate.Replace('~', '\\');
					string fileDirectory = cloudFileNamePlusDate.Substring(0, cloudFileNamePlusDate.LastIndexOf('\\'));

					string currentPath = DATA_PREFIX + fileDirectory; ;
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
				json = wc.DownloadString(string.Concat(OWNCLOUD_URL, "files/api.php?action=getfiles&dir="));
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
						files.Add(new FileInfoX() { CloudName = cloudName, LastModified = modified, CloudNamePlusDate = cloudNamePlusDate, FileName = fileName});
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
			NLogger.Current.Info(string.Format("Uploading {0} [{1} KiB]", localFullPath.Replace(DATA_PATH, ""), kbSize));

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
				string tmpFileName = localFullPath.Replace(DATA_PATH, "").Replace('\\', '~') +
							".enc" + 
							"." + GetUnixTimeStamp(fi.LastWriteTime);
				
				System.IO.File.WriteAllBytes(DATA_PREFIX + tmpFileName, encrypted);

				using (MyWebClient wc = new MyWebClient(200000))
				{
					wc.Headers.Add("Pragma: no-cache");
					wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));

					byte[] response = wc.UploadFile(new Uri(OWNCLOUD_URL + "files/upload.php?dir="), DATA_PREFIX + tmpFileName);
					status = Encoding.ASCII.GetString(response) == "\n\ntrue" ? UploadFileStatus.Success : UploadFileStatus.UnknownError;
				}
				System.IO.File.Delete(DATA_PREFIX + tmpFileName);
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
				wc.Headers.Add(string.Format("Referer: {0}", OWNCLOUD_URL));
				string response = wc.UploadString(OWNCLOUD_URL + "files/api.php", "POST", data3);
				success = response == "true";
			}
			if (success)
				NLogger.Current.Info("Delete Successful");
			else
				NLogger.Current.Warn("Delete Unsuccessful");

			return success;
		}

		public static void DownloadAll()
		{
			foreach (var s in GetFileList())
				Download(s.CloudNamePlusDate);
		}

		public static void DownloadAll(string startsWith)
		{
			var files = GetFileList();
			foreach (var s in files.Where(x => x.CloudName.StartsWith(startsWith)))
				Download(s.CloudNamePlusDate);
		}

	}
}
