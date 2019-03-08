using System.Collections.Generic;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
	public class CapturedException
	{
		public int Code { get; set; }
		public bool Handled { get; set; }
		public string Message { get; set; }
		public List<CapturedStackFrame> Stacktrace { get; set; }
		public string Type { get; set; }
	}
}
