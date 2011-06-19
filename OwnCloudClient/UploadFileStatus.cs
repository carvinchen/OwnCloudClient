using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OwnCloudClient
{
	public enum UploadFileStatus 
	{ 
		Success, 
		NoFileFound, 
		FileTooLarge, 
		UnknownError 
	}
}
