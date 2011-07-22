using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cb.Options
{
	public class Option
	{
		public bool IsRequired { get; set; }
		public bool IsFlag { get; set; }
		public string LongName { get; set; }
		public char ShortName { get; set; }

		public string StringValue { get; set; }
		public bool IsDefined { get; set; }
	}
}
