using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Stacktrace
	{
		[NoTruncationInJsonNet]
		[JsonProperty("Filename")]
		public string FileName { get; set; }

		public string Function { get; set; }

		[JsonProperty("Lineno")]
		public int LineNo { get; set; }

		[NoTruncationInJsonNet]
		public string Module { get; set; }
	}
}
