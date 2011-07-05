using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace OwnCloudClient
{
	public static class Settings
	{
		private static bool GetBool(string SettingName, bool? val)
		{
			if (val.HasValue)
				return val.Value;
			if (ConfigurationManager.AppSettings.AllKeys.Contains(SettingName))
				return ConfigurationManager.AppSettings[SettingName].ToString() == "true";
			return false;
		}

		private static void SetBool(string SettingName, ref bool? var, bool value)
		{
			if (!var.HasValue)
				var = value;
			else
				throw new Exception(string.Format("{0} was specified more than once", SettingName));
		}

		private static int GetInt(string SettingName, int? val, int defaultVal)
		{
			if (val.HasValue)
				return val.Value;
			if (ConfigurationManager.AppSettings.AllKeys.Contains(SettingName))
				return Convert.ToInt32(ConfigurationManager.AppSettings[SettingName].ToString());
			return defaultVal;
		}

		private static void SetInt(string SettingName, ref int? var, int value)
		{
			if (!var.HasValue)
				var = value;
			else
				throw new Exception(string.Format("{0} was specified more than once", SettingName));
		}

		private static string GetString(string SettingName, string val)
		{
			if (!string.IsNullOrEmpty(_password))
				return val;
			if (ConfigurationManager.AppSettings.AllKeys.Contains(SettingName))
				return ConfigurationManager.AppSettings[SettingName].ToString();
			return null;
		}

		private static void SetString(string SettingName, ref string var, string value)
		{
			if (string.IsNullOrEmpty(var))
				var = value;
			else
				throw new Exception(string.Format("{0} was specified more than once", SettingName));
		}

		private static byte[] GetByteArrayFromString(string s)
		{
			string[] ss = s.Replace(" ", "").Split(',');
			byte[] array = new byte[ss.Length];
			for (int i = 0; i < ss.Length; i++)
				array[i] = Convert.ToByte(ss[i]);
			return array;
		}


		public static string WatchDir
		{
			get
			{
				string retVal = GetString("WatchDir", _watchDir);
				if (string.IsNullOrEmpty(retVal))
					retVal = Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar.ToString() + "data" + System.IO.Path.DirectorySeparatorChar.ToString();

				if (!retVal.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
					retVal = retVal + System.IO.Path.DirectorySeparatorChar.ToString();

				return retVal;
			}
			set
			{
				SetString("WatchDir", ref _watchDir, value);
			}
		} private static string _watchDir = null;

		public static string OwnCloudUrl
		{
			get
			{
				string retVal = GetString("OwnCloudUrl", _ownCloudUrl);
				if (string.IsNullOrEmpty(retVal))
					throw new Exception("Cannot find OwnCloud Url");

				if (!retVal.EndsWith("/"))
					retVal = retVal + "/";

				return retVal;
			}
			set
			{
				SetString("OwnCloudUrl", ref _ownCloudUrl, value);
			}
		} private static string _ownCloudUrl = null;

		public static bool NoConfirmDownload
		{
			get { return GetBool("NoConfirmDownload", _noConfirmDownload); }
			set { SetBool("NoConfirmDownload", ref _noConfirmDownload, value); }
		} private static bool? _noConfirmDownload = null;

		public static bool NoConfirmDelete
		{
			get { return GetBool("NoConfirmDelete", _noConfirmDelete); }
			set { SetBool("NoConfirmDelete", ref _noConfirmDelete, value); }
		} private static bool? _noConfirmDelete = null;

		public static bool NoConfirmUpload
		{
			get { return GetBool("NoConfirmUpload", _noConfirmUpload); }
			set { SetBool("NoConfirmUpload", ref _noConfirmUpload, value); }
		} private static bool? _noConfirmUpload = null;

		public static bool RunOnce
		{
			get { return GetBool("RunOnce", _runOnce); }
			set { SetBool("RunOnce", ref _runOnce, value); }
		} private static bool? _runOnce = null;

		public static bool DownloadAll
		{
			get { return GetBool("DownloadAll", _downloadAll); }
			set { SetBool("DownloadAll", ref _downloadAll, value); }
		} private static bool? _downloadAll = null;

		public static bool UploadOnly
		{
			get { return GetBool("UploadOnly", _uploadOnly); }
			set { SetBool("UploadOnly", ref _uploadOnly, value); }
		} private static bool? _uploadOnly = null;

		public static int SleepSeconds
		{
			get { return GetInt("SleepSeconds", _sleepSeconds, 10); }
			set { SetInt("SleepSeconds", ref _sleepSeconds, value); }
		} private static int? _sleepSeconds = null;

		public static string Password
		{
			get { return GetString("Password", _password); }
			set { SetString("Password", ref _password, value); }
		} private static string _password = null;

		public static string UserName
		{
			get { return GetString("UserName", _username); }
			set { SetString("UserName", ref _username, value); }
		} private static string _username = null;
				
		public static byte[] InitilizationVector
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("InitilizationVector"))
					return GetByteArrayFromString(ConfigurationManager.AppSettings["InitilizationVector"].ToString());
				else
					throw new Exception("Cannot find InitilizationVector in config file");
				
			}
		} 

		public static byte[] EncryptionKey
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("EncryptionKey"))
					return GetByteArrayFromString(ConfigurationManager.AppSettings["EncryptionKey"].ToString());
				else
					throw new Exception("Cannot find EncryptionKey in config file");
			}
		}
		
	}
}
