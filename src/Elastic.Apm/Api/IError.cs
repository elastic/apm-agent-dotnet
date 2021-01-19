// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Represents an error which was captured by the agent.
	/// </summary>
	[Specification("docs/spec/v2/error.json")]
	public interface IError
	{
		/// <summary>
		/// The culprit that caused this error.
		/// </summary>
		string Culprit { get; set; }

		/// <summary>
		/// In case the error instance was caused by an exception, this property contains all the
		/// details about the exception.
		/// </summary>
		CapturedException Exception { get; }

		/// <summary>
		/// The Id of the error. This unequally identifies the given error.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Log holds additional information added when the error is logged.
		/// </summary>
		ErrorLog Log { get; set; }

		/// <summary>
		/// The Parent id of the error
		/// </summary>
		string ParentId { get; }

		/// <summary>
		/// The id of the trace where this error happened
		/// </summary>
		string TraceId { get; }

		/// <summary>
		/// The id of the transaction where this error happened
		/// </summary>
		string TransactionId { get; }
	}

	/// <summary>
	/// Represents a log line which is captured as part of an APM error.
	/// </summary>
	public class ErrorLog
	{
		public ErrorLog(string message)
			=> Message = message;

		/// <summary>
		/// The severity of the record.
		/// </summary>
		public string Level { get; set; }

		/// <summary>
		/// The name of the logger instance used.
		/// </summary>
		[JsonProperty("logger_name")]
		public string LoggerName { get; set; }

		/// <summary>
		/// The additionally logged error message.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// A parametrized message. E.g. 'Could not connect to %s'. The property message is still required, and should be equal
		/// to the param_message, but with placeholders replaced. In some situations the param_message is used to group errors
		/// together.
		/// The string is not interpreted, so feel free to use whichever placeholders makes sense in the client languange."
		/// </summary>
		[JsonProperty("param_message")]
		public string ParamMessage { get; set; }

		public List<CapturedStackFrame> StackTrace { get; set; }
	}
}
