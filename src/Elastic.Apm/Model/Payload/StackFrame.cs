using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	public class StackFrame
	{
		[JsonProperty("Filename")]
		public string FileName { get; set; }

		public string Function { get; set; }

		[JsonProperty("Lineno")]
		public int LineNo { get; set; }

		public string Module { get; set; }
	}
}
