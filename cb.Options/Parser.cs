using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cb.Options
{
	public class Parser
	{
		private List<OptionDefinition> _optionDefinitions = new List<OptionDefinition>();
		private List<Option> _options = new List<Option>();

		private void HandleSingleCharOptionString(string s)
		{
			if (s.Length < 2)
				throw new Exception("Too Short");

			OptionDefinition od = _optionDefinitions.Where(d => d.ShortName == s[1]).FirstOrDefault();

			if (od == null)
				throw new Exception("Unknown option " + od.ShortName.ToString());

			if (s.IndexOf('=') != 2)
				throw new Exception("Single letter flag arguments cannot be combined with string args.");

			Option o = new Option() { ShortName = od.ShortName, IsDefined = true, LongName = od.LongName};

			if (!od.IsFlag)
				o.StringValue = s.Substring(s.LastIndexOf('=') + 1);

			_options.Add(o);
		}

		private void HandleMultipleCharFlagOptions(string s)
		{
			for (int i = 1; i < s.Length; i++)
				HandleSingleCharFlagOption(s[i]);
		}

		private void HandleSingleCharFlagOption(char c)
		{
			OptionDefinition od = _optionDefinitions.Where(d => d.ShortName == c).FirstOrDefault();
			if (od == null)
				throw new Exception("Unknown option " + c.ToString());

			if (!od.IsFlag)
				throw new Exception("Attempting to use " + c.ToString() + " as a flag, but it is not a flag");

			_options.Add(new Option() { IsDefined = true, IsFlag = true, LongName = od.LongName, ShortName = od.ShortName });
		}

		private void HandleLongOption(string longOption)
		{
			string argName = longOption.TrimStart(new char[] { '-' });

			if (longOption.Contains('='))
				argName = argName.Substring(0, argName.LastIndexOf('='));
			
			OptionDefinition od = _optionDefinitions.Where(d => string.Compare(d.LongName, argName, true) == 0)
													.FirstOrDefault();

			if (od == null)
				throw new Exception("Unknown option " + argName);

			Option o = new Option() { LongName = argName, IsDefined = true };

			if (!od.IsFlag && longOption.Contains('='))
				o.StringValue = longOption.Substring(longOption.LastIndexOf('=') + 1);

			if (_options.Count(x => (string.Compare(x.LongName, argName, true) == 0)) == 0)
				_options.Add(o);
		}

		public bool IsOptionDefined(string name)
		{
			if (string.IsNullOrEmpty(name))
				return false;

			return _options.Where(o => string.Compare(o.LongName, name, true) == 0)
						   .Count() == 1;
		}

		public string GetOptionStringValue(string name)
		{
			if (string.IsNullOrEmpty(name))
				return string.Empty;

			return _options.Where(o => string.Compare(o.LongName, name, true) == 0)
							.Select(o => o.StringValue)
							.FirstOrDefault();
		}
		
		public void AddDefinition(OptionDefinition od)
		{
			if (string.IsNullOrEmpty(od.LongName))
				throw new Exception("LongName is a required field");

			if (_optionDefinitions.Where(o => string.Compare(o.LongName, od.LongName, true) == 0)
								  .Count() > 0)
				throw new Exception(od.LongName + " is already defined");

			_optionDefinitions.Add(od);
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
						HandleSingleCharFlagOption(s[1]);
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

			var required = _optionDefinitions.Where(od => od.IsRequired == true);
			var undefinedRequired = (from r in required
							 join o in _options on r.LongName.ToLower() equals o.LongName.ToLower() into joinedTable
							 from j in joinedTable.DefaultIfEmpty()
							 where j == null //get all optiondefinitions that are required that don't match a supplied user option
							 select r.LongName).ToList();

			//var LeftJoin = from emp in ListOfEmployees
			//               join dept in ListOfDepartment on emp.DeptID equals dept.ID into JoinedEmpDept
			//               from dept in JoinedEmpDept.DefaultIfEmpty()
			//               select new
			//               {
			//                   EmployeeName = emp.Name,
			//                   DepartmentName = dept != null ? dept.Name : null
			//               };

			if (undefinedRequired.Count > 0)
				throw new Exception(string.Format("{0} is a required option", undefinedRequired[0]));
		}

	}
}
