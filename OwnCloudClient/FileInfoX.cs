using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OwnCloudClient
{
	public class FileInfoX
	{
		public string CloudNamePlusDate { get; set; }
		public string CloudName { get; set; }
		public DateTime LastModified { get; set; }
		public string FileName { get; set; }
		public string EncryptedCloudNamePlusDate { get; set; }
	}
}
