using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class CapturedException
	{
		public string Code { get; set; }
		public bool Handled { get; set; }
		public string Message { get; set; }
		public List<CapturedStackFrame> Stacktrace { get; set; }
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }
	}
}
