using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class CapturedException
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Code { get; set; }

		public bool Handled { get; set; }

		public string Message { get; set; }

		public List<CapturedStackFrame> Stacktrace { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(CapturedException))
		{
			{ "Type", Type },
			{ "Message", Message },
			{ "Handled", Handled },
			{ "Code", Code },
		}.ToString();
	}
}
