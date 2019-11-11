using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// A base interface that encapsulates basic functionality of a piece of work that the agent can measure (e.g.
	/// <see cref="ISpan" /> and <see cref="ITracer" />)
	/// </summary>
	public interface IExecutionSegment
	{
		/// <summary>
		/// The duration of the item.
		/// If it's not set (its HasValue property is false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		double? Duration { get; set; }

		/// <summary>
		/// The id of the item.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// It's true if and only of this segment is sampled.
		/// </summary>
		bool IsSampled { get; }

		/// <summary>
		/// A flat mapping of user-defined labels with string values.
		/// Dots (<code>.</code>) in the keys are not allowed. In case you have a <code>.</code> in your label key, it will be replaced by <code>_</code>.
		/// For example <code>foo.bar</code> will be stored as <code>foo_bar</code>.
		/// </summary>
		/// <exception cref="ArgumentException"><c>null</c> as key is not allowed.</exception>
		Dictionary<string, string> Labels { get; }

		/// <summary>
		/// The name of the item.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Distributed tracing data for this segment as the distributed tracing caller.
		/// </summary>
		DistributedTracingData OutgoingDistributedTracingData { get; }

		/// <summary>
		/// Hex encoded 64 random bits ID of the parent transaction or span.
		/// </summary>
		string ParentId { get; }

		/// <summary>
		/// Hex encoded 128 random bits ID of the correlated trace.
		/// </summary>
		string TraceId { get; }

		/// <summary>
		/// Captures a custom error and reports it to the APM server.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="culprit">The culprit of the error.</param>
		/// <param name="frames">The stack trace when the error occured.</param>
		/// <param name="parentId">
		/// The parent ID that is attached to the error. In case it's null the parent
		/// will be automatically set to the current instance
		/// </param>
		void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null);

		/// <summary>
		/// Captures an exception and reports it to the APM server.
		/// </summary>
		/// <param name="exception">The exception to capture.</param>
		/// <param name="culprit">The value of this parameter is shown as 'Culprit' on the APM UI.</param>
		/// <param name="isHandled">Indicates whether the exception is handled or not.</param>
		/// <param name="parentId">
		/// The parent ID that is attached to the error. In case it's null the parent
		/// will be automatically set to the current instance
		/// </param>
		void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="capturedAction">
		/// The <see cref="Action{ISpan}" /> that points to the code that you want to capture as a span.
		/// The <see cref="ISpan" /> parameter gives you access to the span which is created by this method.
		/// </param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="capturedAction">The <see cref="Action" /> that points to the code that you want to capture as a span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">
		/// The <see cref="Func{ISpan,T}" /> that points to the code with a return value that you want to capture as a span.
		/// The <see cref="ISpan" /> parameter gives you access to the span which is created by this method.
		/// </param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <typeparam name="T">The return type of the code that you want to capture as span.</typeparam>
		/// <returns>
		/// The result of the
		/// <param name="func"></param>
		/// .
		/// </returns>
		T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">
		/// The <see cref="Func{T}" /> that points to the code with a return value that you want to capture as a
		/// span.
		/// </param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <typeparam name="T">The return type of the code that you want to capture as span.</typeparam>
		/// <returns>
		/// The result of the
		/// <param name="func"></param>
		/// .
		/// </returns>
		T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">The <see cref="Func{Task}" /> that points to the async code that you want to capture as a span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">
		/// The <see cref="Func{ISpan,Task}" /> that points to the async code that you want to capture as a span.
		/// The <see cref="ISpan" /> parameter gives you access to the span which is created by this method.
		/// </param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">
		/// The <see cref="Func{Task}" /> that points to the async code with a return value that you want to
		/// capture as a span.
		/// </param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <typeparam name="T">The return type of the <see cref="Task{T}" /> that you want to capture as span.</typeparam>
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
		Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given execution segment and captures unhandled
		/// exceptions
		/// and schedules it to be reported to the APM Server.
		/// The created span will be a child span of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">
		/// The <see cref="Func{ISpan,Task}" /> that points to the async code with a return value that you want to capture as a
		/// span.
		/// The <see cref="ISpan" /> parameter gives you access to the span which is created by this method.
		/// </param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <typeparam name="T">The return type of the <see cref="Task{T}" /> that you want to capture as span.</typeparam>
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
		Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null);

		/// <summary>
		/// Ends the item and schedules it to be reported to the APM Server.
		/// It is illegal to call any methods on a span instance which has already ended.
		/// </summary>
		void End();

		/// <summary>
		/// Start and return a new custom span as a child of this execution segment.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <returns>Returns the newly created and active span.</returns>
		ISpan StartSpan(string name, string type, string subType = null, string action = null);
	}
}
