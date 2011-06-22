using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cb.Options
{
	public class OptionDefinition
	{
		public bool IsRequired { get; set; }
		public bool IsFlag { get; set; }
		public string LongName { get; set; }
	}
}
