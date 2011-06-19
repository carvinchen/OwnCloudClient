using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace OwnCloudClient
{
	public static class NLogger
	{
		private static readonly Logger logger = null;
		static NLogger()
		{
			//logger = LogManager.GetLogger("MyClassName");
			logger = LogManager.GetCurrentClassLogger();
		}
		public static Logger Current { get { return logger; } }
	}
}
