using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OwnCloudClient
{
	public class FileInfoX
	{
		public string CloudFileNameWithEmbeddedData { get; set; }
		public string CloudFileName { get; set; }
		public DateTime LastModified { get; set; }
		public string LocalFileName { get; set; }
		public string EncryptedCloudFileNameWithEmbeddedData { get; set; }
	}
}
