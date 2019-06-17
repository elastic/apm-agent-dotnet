using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class CapturedStackFrame
	{
		[JsonProperty("filename")]
		public string FileName { get; set; }

		public string Function { get; set; }

		/// <summary>
		/// The line number of code part of the stack frame.
		/// Zero value means the actual line number could not have been obtained.
		/// </summary>
		[JsonProperty("lineno")]
		public int LineNo { get; set; }

		public string Module { get; set; }
	}
}
