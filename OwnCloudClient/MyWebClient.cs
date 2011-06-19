using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DirectorySync
{
	public class MyWebClient : WebClient
	{
		/// <summary>
		/// Time in milliseconds
		/// </summary>
		public int Timeout{ get; private set; }	

		public MyWebClient()
		{
			this.Timeout = 60000;
		}

		/// <param name="timeout_ms">Set the connection timeout in milliseconds. Defaults to 60,000</param>
		public MyWebClient(int timeout_ms)
		{
			this.Timeout = timeout_ms;
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			var result = base.GetWebRequest(address);
			result.Timeout = this.Timeout;
			return result;
		}
	}
}
