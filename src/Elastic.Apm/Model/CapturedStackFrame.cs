using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class CapturedStackFrame
	{
		[JsonProperty("filename")]
		public string FileName { get; set; }

		public string Function { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="LineNo" /> because line number is valid only when it is positive.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLineNo() => LineNo > 0;

		/// <summary>
		/// The line number of code part of the stack frame.
		/// Not positive (i.e., zero or negative) value means an actual could not have been obtained.
		/// <seealso cref="ShouldSerializeLineNo" />
		/// </summary>
		[JsonProperty("lineno")]
		public int LineNo { get; set; }

		public string Module { get; set; }
	}
}
