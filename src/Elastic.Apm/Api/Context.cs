using System;
using System.Collections.Generic;

namespace Elastic.Apm.Api
{
	public class Context
	{
		private readonly Lazy<Dictionary<string, string>> tags = new Lazy<Dictionary<string, string>>();

		/// <summary>
		/// If a log record was generated as a result of a http request, the http interface can be used to collect this
		/// information.
		/// This property is by default null! You have to assign a <see cref="Request" /> instance to this property in order to use
		/// it.
		/// </summary>
		public Request Request { get; set; }

		/// <summary>
		/// If a log record was generated as a result of a http request, the http interface can be used to collect this
		/// information.
		/// This property is by default null! You have to assign a <see cref="Response" /> instance to this property in order to use
		/// it.
		/// </summary>
		public Response Response { get; set; }

		public Dictionary<string, string> Tags => tags.Value;
	}
}
