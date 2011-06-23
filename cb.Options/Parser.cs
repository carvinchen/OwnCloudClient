using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cb.Options
{
	public class Parser
	{
		private Dictionary<string, OptionDefinition> _optionDefinitions = new Dictionary<string, OptionDefinition>();
		public Dictionary<string, Option> _options = new Dictionary<string, Option>();

		public bool IsOptionDefined(string name)
		{
			if (!_options.ContainsKey(name))
				return false;
			return _options[name].IsDefined;
		}

		public string GetOptionStringValue(string name)
		{
			if (!_options.ContainsKey(name))
				return string.Empty;
			if (_options[name].IsFlag)
				return string.Empty;
			return _options[name].StringValue;
		}
		
		public void AddDefinition(OptionDefinition od)
		{
			if (!_optionDefinitions.ContainsKey(od.LongName))
				_optionDefinitions.Add(od.LongName, od);
		}

		public void Parse(string[] args)
		{
			foreach (string s in args)
			{
				string name = null;

				if (string.IsNullOrEmpty(s))
					continue;

				if (s.StartsWith("-") || s.StartsWith("/"))
				{
					if (s.Length > 2 && s[1] == '-')
						name = s.Substring(2, s.Length - 2);
					
					//ignore one hyphen auguments for now.
					//if needed, they can be chained together ls -lw
					//should they be able to set?  ls -p=5
				}
				
				if (s.Contains('='))
					name = name.Substring(0, name.LastIndexOf('='));

				if (string.IsNullOrEmpty(name) || !_optionDefinitions.ContainsKey(name))
					throw new Exception("Unknown option " + name);

				Option o = new Option() { LongName = name, IsDefined = true };

				if (!_optionDefinitions[name].IsFlag && s.Contains('='))
					o.StringValue = s.Substring(s.LastIndexOf('=') + 1);

				if (!_options.ContainsKey(o.LongName))
					_options.Add(o.LongName, o);
				else
					_options[name] = o;
			}

			var required = _optionDefinitions.Where( x => x.Value.IsRequired == true);
			var undefined = required.Where(y => !_options.ContainsKey(y.Key)).ToList();

			if (undefined.Count > 0)
				throw new Exception(string.Format("{0} is a required option", undefined[0].Value.LongName));
		}
	}
}
