using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectorySync
{
	public enum UploadFileStatus 
	{ 
		Success, 
		NoFileFound, 
		FileTooLarge, 
		UnknownError 
	}
}
