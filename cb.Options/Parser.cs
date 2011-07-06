using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cb.Options
{
	public class Parser
	{
		private Dictionary<string, OptionDefinition> _longOptionDefinitions = new Dictionary<string, OptionDefinition>();
		private Dictionary<string, Option> _longOptions = new Dictionary<string, Option>();

		private Dictionary<char, OptionDefinition> _shortOptionDefinitions = new Dictionary<char, OptionDefinition>();
		private Dictionary<char, Option> _shortOptions = new Dictionary<char, Option>();

		private void HandleSingleCharOptionString(string s)
		{
			if (s.Length == 2 && !_shortOptionDefinitions.ContainsKey(s[1]))
				throw new Exception("Unknown option " + s[1].ToString());

			if (s.IndexOf('=') != 2)
				throw new Exception("Single letter flag arguments cannot be combined with string args.");

			if (!_shortOptionDefinitions.ContainsKey(s[1]))
				throw new Exception("Unknown option " + s[1].ToString());

			Option o = new Option() { ShortName = s[1], IsDefined = true };

			if (!_shortOptionDefinitions[s[1]].IsFlag)
				o.StringValue = s.Substring(s.LastIndexOf('=') + 1);

			_shortOptions.Add(s[1], o);
		}

		private void HandleMultipleCharFlagOptions(string s)
		{
			for (int i = 1; i < s.Length; i++)
			{
				if (!_shortOptionDefinitions.ContainsKey(s[i]))
					throw new Exception("Unknown option " + s[i].ToString());

				if (!_shortOptionDefinitions[s[i]].IsFlag)
					throw new Exception("Attempting to use " + s[i].ToString() + " as a flag, but it is not a flag");

				if (_shortOptionDefinitions.Keys.Contains<char>(s[i]))
					_shortOptions.Add(s[i], new Option() { IsDefined = true, IsFlag = true });
			}
		}

		private void HandleSingleCharFlagOption(string s)
		{
			if (!_shortOptionDefinitions.ContainsKey(s[1]))
				throw new Exception("Unknown option " + s[1].ToString());

			if (s.Length == 2 && !_shortOptionDefinitions[s[1]].IsFlag)
				throw new Exception("Attempting to use " + s[1].ToString() + " as a flag, but it is not a flag");

			if (s.Length == 2 && _shortOptionDefinitions.Keys.Contains<char>(s[1]))
				_shortOptions.Add(s[1], new Option() { IsDefined = true, IsFlag = true });
		}

		private void HandleLongOption(string s)
		{
			string name = s.Substring(2, s.Length - 2);

			if (s.Contains('='))
				name = name.Substring(0, name.LastIndexOf('='));

			if (string.IsNullOrEmpty(name) || !_longOptionDefinitions.ContainsKey(name))
				throw new Exception("Unknown option " + name);

			Option o = new Option() { LongName = name, IsDefined = true };

			if (!_longOptionDefinitions[name].IsFlag && s.Contains('='))
				o.StringValue = s.Substring(s.LastIndexOf('=') + 1);

			if (!_longOptions.ContainsKey(o.LongName))
				_longOptions.Add(o.LongName, o);
		}

		public bool IsOptionDefined(string name)
		{
			if (string.IsNullOrEmpty(name))
				return false;

			if (_longOptionDefinitions.ContainsKey(name))
			{
				Option o = null;
				if (_longOptions.ContainsKey(name))
					o = _longOptions[name];

				return (o != null && o.IsDefined) ||
					(_shortOptions.ContainsKey(_longOptionDefinitions[name].ShortName)
							&& _shortOptions[_longOptionDefinitions[name].ShortName].IsDefined);
			}
			return false;
		}

		public string GetOptionStringValue(string name)
		{
			if (string.IsNullOrEmpty(name))
				return string.Empty;

			if (!_longOptionDefinitions.ContainsKey(name))
				return string.Empty;

			if (_longOptions.ContainsKey(name) && _longOptions[name].IsFlag)
				return string.Empty;

			if (_shortOptions.ContainsKey(_longOptionDefinitions[name].ShortName) && _shortOptions[_longOptionDefinitions[name].ShortName].IsFlag)
				return string.Empty;

			if (_longOptions.ContainsKey(name))
				return _longOptions[name].StringValue;

			if (_shortOptions.ContainsKey(_longOptionDefinitions[name].ShortName))
				return _shortOptions[_longOptionDefinitions[name].ShortName].StringValue;

			return string.Empty;
		}
		
		public void AddDefinition(OptionDefinition od)
		{
			if (string.IsNullOrEmpty(od.LongName))
				throw new Exception("LongName is a required field");
			
			//add to long index
			if (!_longOptionDefinitions.ContainsKey(od.LongName))
				_longOptionDefinitions.Add(od.LongName, od);

			//add to short index
			if (!_shortOptionDefinitions.ContainsKey(od.ShortName))
				_shortOptionDefinitions.Add(od.ShortName, od);
		}

		public void Parse(string[] args)
		{
			foreach (string s in args)
			{
				if (string.IsNullOrEmpty(s))
					continue;

				if (s.StartsWith("--"))
				{
					HandleLongOption(s);
				}
				else if (s.StartsWith("-"))
				{
					if (s.Length == 2)
					{
						HandleSingleCharFlagOption(s);
					}
					else if (s.Length > 2)
					{
						if (s.Contains('='))
							HandleSingleCharOptionString(s);
						else
							HandleMultipleCharFlagOptions(s);
					}
				}
				else
				{
					throw new Exception("Options must start with '-'");
				}
			}

			//_longOptionDefinitions is a complete list since longName is a required field
			var required = _longOptionDefinitions.Where(x => x.Value.IsRequired == true);
			var undefined = required.Where(y => !_longOptions.ContainsKey(y.Value.LongName) && !_shortOptions.ContainsKey(y.Value.ShortName)).ToList();

			if (undefined.Count > 0)
				throw new Exception(string.Format("{0} is a required option", undefined[0].Value.LongName));
		}

	}
}
