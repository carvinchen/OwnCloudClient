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

		private static string EmbedDataIntoFileName(string localFullPath, DateTime lastModified)
		{
			return localFullPath.Replace(Settings.WatchDir, "").Replace(System.IO.Path.DirectorySeparatorChar, '~') +
							".enc" +
							"." + GetUnixTimeStamp(lastModified);
		}

		//!FileNameProcessing
		private static string GetLocalFilePathFromCloudFileNameWithEmbeddedData(string cloudFileNameWithEmbeddedData)
		{
			return Settings.WatchDir + cloudFileNameWithEmbeddedData.Substring(0, cloudFileNameWithEmbeddedData.Length - (4 + 13));
		}

		//!FileNameProcessing
		private static DateTime GetLastModifiedFromCloudFileNameWithEmbeddedData(string cloudFileNameWithEmbeddedData)
		{
			string sUnixTime = cloudFileNameWithEmbeddedData.Substring(cloudFileNameWithEmbeddedData.Length - 12, 12);
			return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToDouble(sUnixTime));
		}

		//!FileNameProcessing
		private static FileInfoX CreateFileInfoXFromcloudFileNameWithEmbeddedData(string cloudFileNameWithEmbeddedData)
		{
			string cloudFileName = cloudFileNameWithEmbeddedData.Substring(0, cloudFileNameWithEmbeddedData.Length - 13); //13 for '.' + unix date

			string sUnixTime = cloudFileNameWithEmbeddedData.Substring(cloudFileNameWithEmbeddedData.Length - 12, 12);
			DateTime modified = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToDouble(sUnixTime));

			string localFileName = cloudFileName.Replace('~', System.IO.Path.DirectorySeparatorChar);
			localFileName = localFileName.Substring(0, localFileName.Length - 4);

			if (!string.IsNullOrEmpty(cloudFileNameWithEmbeddedData))
				return new FileInfoX() 
				{ 
					CloudFileName = cloudFileName, 
					LastModified = modified, 
					CloudFileNameWithEmbeddedData = cloudFileNameWithEmbeddedData, 
					LocalFileName = localFileName,
					EncryptedCloudFileNameWithEmbeddedData = EncryptFileName(cloudFileNameWithEmbeddedData)
				};
			return null;
		}

		private static string EncryptFileName(string fileName)
		{
			byte[] bs = Encoding.ASCII.GetBytes(fileName);
			return Convert.ToBase64String(Encryption.EncryptBytes(bs)).Replace('/', '-').Replace('+', '_');
		}

		private static string DecryptFileName(string encryptedFileName)
		{
			byte[] bs = Convert.FromBase64String(encryptedFileName.Replace('-', '/').Replace('_', '+'));
			return Encoding.ASCII.GetString(Encryption.DecryptBytes(bs));
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

		//http://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
		private static IEnumerable<string> GetFiles(string path)
		{
			Queue<string> queue = new Queue<string>();
			queue.Enqueue(path);
			while (queue.Count > 0)
			{
				path = queue.Dequeue();
				try
				{
					foreach (string subDir in Directory.GetDirectories(path))
					{
						queue.Enqueue(subDir);
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
				}

				string[] files = null;
				try
				{
					files = Directory.GetFiles(path);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
				}

				if (files != null)
				{
					for (int i = 0; i < files.Length; i++)
						yield return files[i];
				}
			}
		}

		private static string GetUnixTimeStamp(DateTime modifiedDate)
		{
			TimeSpan ts = (modifiedDate - new DateTime(1970, 1, 1, 0, 0, 0));
			return string.Format("{0:000000000000}", ts.TotalSeconds); //12 zeros
		}


		public static bool Login(string userName, string password)
		{
			phpId = GetPhpId();
			NLogger.Current.Trace(string.Format("PhpId: {0}", phpId));
			bool success = false;

			StringBuilder sb = new StringBuilder();

			sb.Append("-----------------------------7db2172440460\r\n");
			sb.Append("Content-Disposition: form-data; name=\"login\"\r\n");
			sb.Append("\r\n");
			sb.Append(userName + "\r\n");
			sb.Append("-----------------------------7db2172440460\r\n");
			sb.Append("Content-Disposition: form-data; name=\"password\"\r\n");
			sb.Append("\r\n");
			sb.Append(password + "\r\n");
			sb.Append("-----------------------------7db2172440460\r\n");
			sb.Append("Content-Disposition: form-data; name=\"loginbutton\"\r\n");
			sb.Append("\r\n");
			sb.Append("login\r\n");
			sb.Append("-----------------------------7db2172440460--\r\n");

			WebRequest request = WebRequest.Create(Settings.OwnCloudUrl);
			((HttpWebRequest)request).AllowAutoRedirect = false;

			//http://www.velocityreviews.com/forums/t302174-why-do-i-get-the-server-committed-a-protocol-violation.html
			((HttpWebRequest)request).KeepAlive = false;

			request.Method = "POST";
			request.ContentType = "multipart/form-data; boundary=---------------------------7db2172440460\r\n";
			request.Headers.Add("Pragma: no-cache");
			request.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));

			using (Stream dataStream = request.GetRequestStream())
			{
				byte[] datums = Encoding.ASCII.GetBytes(sb.ToString());
				dataStream.Write(datums, 0, datums.Length);
				dataStream.Close();
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.Found)
			{
				using (Stream responseStream = response.GetResponseStream())
				{
					using (StreamReader reader = new StreamReader(responseStream))
					{
						string responseFromServer = reader.ReadToEnd();
						success = !string.IsNullOrWhiteSpace(responseFromServer)
									&& responseFromServer.Length >= 4
									&& responseFromServer.Substring(0,4) == "\n\n12"; //success message
						reader.Close();
					}
					responseStream.Close();
				}
				response.Close();
			}
			return success;
		}

		public static List<FileInfoX> GetRemoteFileList()
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
					string cloudNamePlusDate = DecryptFileName(x.SelectToken("name").ToString().Trim(new char[] { '\"' }));

					FileInfoX fix = CreateFileInfoXFromcloudFileNameWithEmbeddedData(cloudNamePlusDate);
					if (x != null)
						files.Add(fix);
				}
			}

			return files;
		}

		public static List<FileInfoX> GetLocalFileList()
		{
			List<FileInfoX> files = new List<FileInfoX>();
			
			foreach (var item in GetFiles(Settings.WatchDir))
			{
				if (!System.IO.File.Exists(item))
					continue;
				else if (System.Text.RegularExpressions.Regex.IsMatch(item, @"\.enc\.\d{12}$"))
					continue;

				var info = new System.IO.FileInfo(item);

				FileInfoX x = new FileInfoX();
				DateTime lastWrite = new DateTime(info.LastWriteTime.Year, info.LastWriteTime.Month, info.LastWriteTime.Day, info.LastWriteTime.Hour, info.LastWriteTime.Minute, info.LastWriteTime.Second); //do it this way to avoid milisecond comparison problemsinfo.LastWriteTime;

				//!FileNameProcessing
				x.CloudFileName = item.Replace(Settings.WatchDir, "").Replace(System.IO.Path.DirectorySeparatorChar, '~') + ".enc";
				x.LastModified = lastWrite;
				x.LocalFileName = item.Replace(Settings.WatchDir, "");
				x.CloudFileNameWithEmbeddedData = x.CloudFileName + "." + OwnCloudClient.GetUnixTimeStamp(lastWrite);
				x.EncryptedCloudFileNameWithEmbeddedData = EncryptFileName(x.CloudFileNameWithEmbeddedData);
				files.Add(x);
			}
			return files;
		}

		public static void PrintRemoteFileList()
		{
			IOrderedEnumerable<FileInfoX> files = GetRemoteFileList().OrderBy(x => x.CloudFileName);
			foreach (var f in files)
				Console.WriteLine(f.LocalFileName);
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
				string cloudFileNameWithEmbeddedData = EmbedDataIntoFileName(localFullPath, fi.LastWriteTime);

				string encryptedFileName = EncryptFileName(cloudFileNameWithEmbeddedData);

				System.IO.File.WriteAllBytes(Settings.WatchDir + encryptedFileName, encrypted);

				using (MyWebClient wc = new MyWebClient(200000))
				{
					wc.Headers.Add("Pragma: no-cache");
					wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));

					byte[] response = wc.UploadFile(new Uri(Settings.OwnCloudUrl + "files/upload.php?dir="), Settings.WatchDir + encryptedFileName);
					status = Encoding.ASCII.GetString(response) == "\n\ntrue" ? UploadFileStatus.Success : UploadFileStatus.UnknownError;
				}

				System.IO.File.Delete(Settings.WatchDir + encryptedFileName);
				
			}

			if (status == UploadFileStatus.Success)
				NLogger.Current.Info("Upload Successful.");
			else
				NLogger.Current.Warn("Upload Unsuccessful.");
			return status;
		}

		public static bool DeleteFile(string cloudFileNameWithEmbeddedData)
		{
			bool success = false;
			using (WebClient wc = new WebClient())
			{
				NLogger.Current.Info(string.Format("Deleting {0}", cloudFileNameWithEmbeddedData));
				string data3 = string.Format("action=delete&dir=&file={0}", EncryptFileName(cloudFileNameWithEmbeddedData));
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

		public static void DownloadFile(string cloudFileNameWithEmbeddedData)
		{
			try
			{
				using (WebClient wc = new WebClient())
				{
					wc.Headers.Add("Pragma: no-cache");
					wc.Headers.Add(string.Format("Cookie: PHPSESSID={0}", phpId));
					NLogger.Current.Info(string.Format("Downloading {0}", cloudFileNameWithEmbeddedData));
					Uri uri = new Uri(string.Concat(Settings.OwnCloudUrl, "files/api.php?action=get&dir=&file=", EncryptFileName(cloudFileNameWithEmbeddedData))); //cbTODO: urlencode filename?
					wc.DownloadFile(uri, Settings.WatchDir + cloudFileNameWithEmbeddedData);
				}

				byte[] decryptedContents = Encryption.DecryptFile(Settings.WatchDir + cloudFileNameWithEmbeddedData);
				System.IO.File.Delete(Settings.WatchDir + cloudFileNameWithEmbeddedData);

				if (cloudFileNameWithEmbeddedData.Contains('~'))
				{
					cloudFileNameWithEmbeddedData = cloudFileNameWithEmbeddedData.Replace('~', System.IO.Path.DirectorySeparatorChar);
					string fileDirectory = cloudFileNameWithEmbeddedData.Substring(0, cloudFileNameWithEmbeddedData.LastIndexOf(System.IO.Path.DirectorySeparatorChar.ToString()));

					string currentPath = Settings.WatchDir + fileDirectory; ;
					if (!System.IO.Directory.Exists(currentPath))
						System.IO.Directory.CreateDirectory(currentPath);
				}

				string localFileName = GetLocalFilePathFromCloudFileNameWithEmbeddedData(cloudFileNameWithEmbeddedData);
				System.IO.File.WriteAllBytes(localFileName, decryptedContents);

				DateTime modified = GetLastModifiedFromCloudFileNameWithEmbeddedData(cloudFileNameWithEmbeddedData);
				File.SetLastWriteTime(localFileName, modified);

				NLogger.Current.Info("Download finished.");
			}
			catch (Exception ex)
			{
				NLogger.Current.ErrorException(string.Format("Problem downloading {0}.", cloudFileNameWithEmbeddedData), ex);
			}
		}

		public static bool ShouldDownload(FileInfoX f)
		{
			string fname = Settings.WatchDir + f.LocalFileName;
			bool shouldDownload = false;

			if (System.IO.File.Exists(fname))
			{
				System.IO.FileInfo info = new FileInfo(fname);
				shouldDownload = info.LastWriteTime < f.LastModified;
			}
			else
			{
				shouldDownload = true;
			}
			return shouldDownload;
		}

		public static void DownloadAll()
		{
			foreach (var f in GetRemoteFileList())
			{
				if (ShouldDownload(f))
					OwnCloudClient.DownloadFile(f.CloudFileNameWithEmbeddedData);
			}
		}

		public static void DownloadAll(string startsWith)
		{
			var files = GetRemoteFileList();
			foreach (var f in files.Where(x => x.CloudFileName.StartsWith(startsWith)))
			{
				if (ShouldDownload(f))
					DownloadFile(f.CloudFileNameWithEmbeddedData);
			}
		}

	}
}
