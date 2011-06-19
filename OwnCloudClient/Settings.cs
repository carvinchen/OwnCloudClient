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
				if (ConfigurationManager.AppSettings.AllKeys.Contains("NoConfirmDownload"))
					return ConfigurationManager.AppSettings["NoConfirmDownload"].ToString() == "true";
				return false;
			}
		}

		public static bool NoConfirmDelete
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("NoConfirmDelete"))
					return ConfigurationManager.AppSettings["NoConfirmDelete"].ToString() == "true";
				return false;
			}
		}

		public static bool NoConfirmUpload
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("NoConfirmUpload"))
					return ConfigurationManager.AppSettings["NoConfirmUpload"].ToString() == "true";
				return false;
			}
		}

		public static bool RunOnce
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("RunOnce"))
					return ConfigurationManager.AppSettings["RunOnce"].ToString() == "true";
				return false;
			}
		}
		public static bool MassDownload
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("MassDownload"))
					return ConfigurationManager.AppSettings["MassDownload"].ToString() == "true";
				return false;
			}
		}

		public static int SleepSeconds
		{
			get
			{
				if (ConfigurationManager.AppSettings.AllKeys.Contains("SleepSeconds"))
					return Convert.ToInt32(ConfigurationManager.AppSettings["SleepSeconds"].ToString());
				return 10;
			}
		}
	}
}
