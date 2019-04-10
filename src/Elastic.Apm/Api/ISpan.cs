using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elastic.Apm.Api
{
	public interface ISpan
	{
		/// <summary>
		/// The action of the span.
		/// Examples: 'query'.
		/// </summary>
		string Action { get; set; }

		/// <summary>
		/// Any other arbitrary data captured by the agent, optionally provided by the user.
		/// </summary>
		SpanContext Context { get; }

		/// <summary>
		/// The duration of the span.
		/// If it's not set (its HasValue property is false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		double? Duration { get; set; }

		/// <summary>
		/// The id of the span.
		/// </summary>
		string Id { get; set; }

		/// <summary>
		/// The name of the span.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Hex encoded 64 random bits ID of the parent transaction or span.
		/// </summary>
		string ParentId { get; }

		/// <summary>
		/// The subtype of the span.
		/// Examples: 'http', 'mssql'.
		/// </summary>
		string Subtype { get; set; }

		/// <summary>
		/// A flat mapping of user-defined tags with string values.
		/// </summary>
		Dictionary<string, string> Tags { get; }

		/// <summary>
		/// The timestamp of the span
		/// </summary>
		long Timestamp { get; }

		/// <summary>
		/// Hex encoded 128 random bits ID of the correlated trace.
		/// </summary>
		string TraceId { get; }

		/// <summary>
		/// Hex encoded 64 random bits ID of the correlated transaction.
		/// </summary>
		string TransactionId { get; }

		/// <summary>
		/// The type of the span.
		/// Examples: 'db', 'external'.
		/// </summary>
		string Type { get; set; }

		/// <summary>
		/// Captures a custom error and reports it to the APM server.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="culprit">The culprit of the error.</param>
		/// <param name="frames">The stack trace when the error occured.</param>
		void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null);

		/// <summary>
		/// Captures an exception and reports it to the APM server.
		/// </summary>
		/// <param name="exception">The exception to capture.</param>
		/// <param name="culprit">The value of this parameter is shown as 'Culprit' on the APM UI.</param>
		void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null);

		/// <summary>
		/// Start and return a new custom span as a child of this span.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <returns>Returns the newly created and active span.</returns>
		ISpan StartSpan(string name, string type, string subType = null, string action = null);

		/// <summary>
		/// Ends the span and schedules it to be reported to the APM Server.
		/// It is illegal to call any methods on a span instance which has already ended.
		/// </summary>
		void End();
	}
}
