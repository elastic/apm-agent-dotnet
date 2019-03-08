using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class CapturedStackFrame
	{
		[JsonProperty("filename")]
		public string FileName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Function { get; set; }

		[JsonProperty("lineno")]
		public int LineNo { get; set; }

		public string Module { get; set; }
	}
}
