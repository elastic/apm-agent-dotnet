using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Stacktrace
	{

		[JsonProperty("Filename")]
		public string FileName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Function { get; set; }

		[JsonProperty("Lineno")]
		public int LineNo { get; set; }

		public string Module { get; set; }
	}
}
