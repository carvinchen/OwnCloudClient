using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace OwnCloudClient
{
	public static class Settings
	{
		public static string WatchDir
		{
			get
			{
				string retVal = "";

				if (_watchDir != null)
					retVal = _watchDir;
				else if (ConfigurationManager.AppSettings.AllKeys.Contains("WatchDir"))
					retVal = ConfigurationManager.AppSettings["WatchDir"].ToString();
				else
					retVal = Environment.CurrentDirectory + "\\data\\";

				if (!retVal.EndsWith("\\") && !retVal.EndsWith("/"))
					retVal = retVal + "\\";

				return retVal;
			}
			set
			{
				if (_watchDir != null)
					throw new Exception("WatchDir was specified more than once");
				_watchDir = value;
			}
		} private static string _watchDir = null;

		public static string OwnCloudUrl
		{
			get
			{
				string retVal = "";

				if (_ownCloudUrl != null)
					retVal = _ownCloudUrl;
				else if (ConfigurationManager.AppSettings.AllKeys.Contains("OwnCloudUrl"))
					retVal = ConfigurationManager.AppSettings["OwnCloudUrl"].ToString();
				else
					throw new Exception("Cannot find url in config file");

				if (!retVal.EndsWith("/"))
					retVal = retVal + "/";

				return retVal;
			}
			set
			{
				if (_ownCloudUrl != null)
					throw new Exception("OwnCloudUrl was specified more than once");
				_ownCloudUrl = value;
			}
		} private static string _ownCloudUrl = null;

		public static bool NoConfirmDownload
		{
			get
			{
				if (_noConfirmDownload.HasValue)
					return _noConfirmDownload.Value;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("NoConfirmDownload"))
					return ConfigurationManager.AppSettings["NoConfirmDownload"].ToString() == "true";
				return false;
			}
			set
			{
				if (!_noConfirmDownload.HasValue)
					_noConfirmDownload = value;
				else
					throw new Exception("NoConfirmDownload was specified more than once");
			}
		} private static bool? _noConfirmDownload = null;

		public static bool NoConfirmDelete
		{
			get
			{
				if (_noConfirmDelete.HasValue)
					return _noConfirmDelete.Value;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("NoConfirmDelete"))
					return ConfigurationManager.AppSettings["NoConfirmDelete"].ToString() == "true";
				return false;
			}
			set
			{
				if (!_noConfirmDelete.HasValue)
					_noConfirmDelete = value;
				else
					throw new Exception("NoConfirmDelete was specified more than once");
			}
		} private static bool? _noConfirmDelete = null;

		public static bool NoConfirmUpload
		{
			get
			{
				if (_noConfirmUpload.HasValue)
					return _noConfirmUpload.Value;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("NoConfirmUpload"))
					return ConfigurationManager.AppSettings["NoConfirmUpload"].ToString() == "true";
				return false;
			}
			set
			{
				if (!_noConfirmUpload.HasValue)
					_noConfirmUpload = value;
				else
					throw new Exception("NoConfirmUpload was specified more than once");
			}
		} private static bool? _noConfirmUpload = null;

		public static bool RunOnce
		{
			get
			{
				if (_runOnce.HasValue)
					return _runOnce.Value;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("RunOnce"))
					return ConfigurationManager.AppSettings["RunOnce"].ToString() == "true";
				return false;
			}
			set
			{
				if (!_runOnce.HasValue)
					_runOnce = value;
				else
					throw new Exception("RunOnce was specified more than once");
			}
		} private static bool? _runOnce = null;

		public static bool DownloadOnly
		{
			get
			{
				if (_downloadOnly.HasValue)
					return _downloadOnly.Value;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("DownloadOnly"))
					return ConfigurationManager.AppSettings["DownloadOnly"].ToString() == "true";
				return false;
			}
			set
			{
				if (!_downloadOnly.HasValue)
					_downloadOnly = value;
				else
					throw new Exception("DownloadOnly was specified more than once");
			}
		} private static bool? _downloadOnly = null;

		public static int SleepSeconds
		{
			get
			{
				if (_sleepSeconds.HasValue)
					return _sleepSeconds.Value;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("SleepSeconds"))
					return Convert.ToInt32(ConfigurationManager.AppSettings["SleepSeconds"].ToString());
				return 10;
			}
			set
			{
				if (!_sleepSeconds.HasValue)
					_sleepSeconds = value;
				else
					throw new Exception("SleepSeconds was specified more than once");
			}
		} private static int? _sleepSeconds = null;

		public static string Password
		{
			get
			{
				if (!string.IsNullOrEmpty(_password))
					return _password;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("Password"))
					return ConfigurationManager.AppSettings["Password"].ToString();
				return null;
			}
			set
			{
				if (string.IsNullOrEmpty(_password))
					_password = value;
				else
					throw new Exception("Password was specified more than once");
			}
		} private static string _password = null;

		public static string UserName
		{
			get
			{
				if (!string.IsNullOrEmpty(_password))
					return _username;
				if (ConfigurationManager.AppSettings.AllKeys.Contains("UserName"))
					return ConfigurationManager.AppSettings["UserName"].ToString();
				return null;
			}
			set
			{
				if (string.IsNullOrEmpty(_username))
					_username = value;
				else
					throw new Exception("UserName was specified more than once");
			}
		} private static string _username = null;
		
	}
}
