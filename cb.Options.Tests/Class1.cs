using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace cb.Options.Tests
{
	public class MyTests
	{
		[Fact]
		public void Normal()
		{
			Parser p = new Parser();
			p.AddDefinition(new OptionDefinition() { IsFlag = true, LongName = "abc" });
			p.Parse(new string[] { "--abc" });
			Assert.Equal<bool>(true, p.IsOptionDefined("abc"));
		}

		[Fact]
		public void MissingRequired()
		{
			Assert.Throws<Exception>(delegate
			{
				Parser p = new Parser();
				p.AddDefinition(new OptionDefinition() { IsFlag = true, LongName = "abc", IsRequired = true });
				p.Parse(new string[] { "" });
			});
		}

		[Fact]
		public void UnknownOption()
		{
			Assert.Throws<Exception>(delegate
			{
				Parser p = new Parser();
				p.AddDefinition(new OptionDefinition() { IsFlag = true, LongName = "abc", IsRequired = true });
				p.Parse(new string[] { "--aaa" });
			});
		}

		[Fact]
		public void GetValue()
		{
			Parser p = new Parser();
			p.AddDefinition(new OptionDefinition() { IsFlag = false, LongName = "abc" });
			p.Parse(new string[] { "--abc=def" });
			Assert.Equal<bool>(true, p.IsOptionDefined("abc"));
			Assert.Equal<string>("def", p.GetOptionStringValue("abc"));
		}

		[Fact]
		public void ShortTest1()
		{
			Parser p = new Parser();
			p.AddDefinition(new OptionDefinition() { IsFlag = true, ShortName = 'a', LongName = "abc" });
			p.Parse(new string[] { "-a" });
			Assert.Equal<bool>(true, p.IsOptionDefined("abc"));
		}

		[Fact]
		public void ShortTest2()
		{
			Parser p = new Parser();
			p.AddDefinition(new OptionDefinition() { IsFlag = true, ShortName = 'a', LongName = "abc" });
			p.AddDefinition(new OptionDefinition() { IsFlag = true, ShortName = 'b', LongName = "bc" });
			p.Parse(new string[] { "-ab" });
			Assert.Equal<bool>(true, p.IsOptionDefined("abc"));
			Assert.Equal<bool>(true, p.IsOptionDefined("bc"));
		}

		[Fact]
		public void ShortTest3()
		{
			Assert.Throws<Exception>(delegate
				{
					Parser p = new Parser();
					p.AddDefinition(new OptionDefinition() { IsFlag = false, ShortName = 'a', LongName = "abc" });
					p.AddDefinition(new OptionDefinition() { IsFlag = false, ShortName = 'b', LongName = "bc" });
					p.Parse(new string[] { "-ab=c" });
					Assert.Equal<bool>(true, p.IsOptionDefined("abc"));
					Assert.Equal<bool>(true, p.IsOptionDefined("bc"));
				});
		}

		[Fact]
		public void ShortTest4()
		{
			Assert.Throws<Exception>(delegate
			{
				Parser p = new Parser();
				p.AddDefinition(new OptionDefinition() { IsFlag = true, ShortName = 'a', LongName = "abc" });
				p.AddDefinition(new OptionDefinition() { IsFlag = false, ShortName = 'b', LongName = "bc" , IsRequired = true});
				p.AddDefinition(new OptionDefinition() { IsFlag = false, ShortName = 'c', LongName = "bcc" });
				p.Parse(new string[] { "-ac" });
				Assert.Equal<bool>(true, p.IsOptionDefined("a"));
			});
		}

		[Fact]
		public void ShortTest5()
		{
				Parser p = new Parser();
				p.AddDefinition(new OptionDefinition() { IsFlag = true, ShortName = '1', LongName = "runonce" });
				p.AddDefinition(new OptionDefinition() { IsFlag = true, ShortName = 'f', LongName = "flag", IsRequired = true });
				p.AddDefinition(new OptionDefinition() { IsFlag = false, ShortName = 'x', LongName = "bcc" });
				p.Parse(new string[] { "-1f", "-x=tt" });
				Assert.Equal<bool>(true, p.IsOptionDefined("runonce"));
				Assert.Equal<bool>(true, p.IsOptionDefined("flag"));
				Assert.Equal<string>("tt", p.GetOptionStringValue("bcc"));

		}


	}
}
