// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Information about the original error
	/// </summary>
	public class CapturedException
	{
		/// <summary>
		/// A collection of error exceptions representing chained exceptions.
		/// The chain starts with the outermost exception, followed by its cause, and so on.
		/// </summary>
		public List<CapturedException> Cause { get; set; }

		/// <summary>
		/// Code that is set when the error happened, e.g. database error code.
		/// </summary>
		[MaxLength]
		public string Code { get; set; }

		/// <summary>
		/// Indicates whether the error was caught in the code or not.
		/// </summary>
		// TODO: makes this nullable in 2.x
		public bool Handled { get; set; }

		/// <summary>
		/// The originally captured error message.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Stacktrace information of the captured exception.
		/// </summary>
		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		/// <summary>
		/// The type of the exception
		/// </summary>
		[MaxLength]
		public string Type { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(CapturedException))
		{
			{ nameof(Type), Type }, { nameof(Message), Message }, { nameof(Handled), Handled }, { nameof(Code), Code }
		}.ToString();
	}
}
