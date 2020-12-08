// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class CapturedStackFrame
	{
		/// <summary>
		/// The absolute path of the file involved in the stack frame.
		/// </summary>
		[JsonProperty("abs_path")]
		public string AbsPath { get; set; }

		[JsonProperty("classname")]
		public string ClassName { get; set; }

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
