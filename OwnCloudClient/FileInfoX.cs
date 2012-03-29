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
		public DateTime LastModifiedUtc { get; set; }
		public string LocalFileName { get; set; }
		public string EncryptedCloudFileNameWithEmbeddedData { get; set; }
	}
}
