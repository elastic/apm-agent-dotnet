using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Elastic.Apm.Api
{
	public interface ITransaction
	{
		/// <summary>
		/// Any arbitrary contextual information regarding the event, captured by the agent, optionally provided by the user.
		/// This field is lazily initialized, you don't have to assign a value to it and you don't have to null check it either.
		/// </summary>
		Context Context { get; }

		/// <summary>
		/// The duration of the transaction.
		/// If it's not set (its HasValue property is false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		double? Duration { get; set; }

		Guid Id { get; }

		/// <summary>
		/// The name of the span.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		string Result { get; set; }

		/// <summary>
		/// A flat mapping of user-defined tags with string values.
		/// </summary>
		Dictionary<string, string> Tags { get; }

		/// <summary>
		/// The type of the transaction.
		/// Example: 'request'
		/// </summary>
		string Type { get; set; }

		/// <summary>
		/// Captures a custom error and reports it to the APM server.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="culprit">The culprit of the error.</param>
		/// <param name="frames">The stack trace when the error occured.</param>
		void CaptureError(string message, string culprit, StackFrame[] frames);

		/// <summary>
		/// Captures an exception and reports it to the APM server.
		/// </summary>
		/// <param name="exception">The exception to capture.</param>
		/// <param name="culprit">The value of this parameter is shown as 'Culprit' on the APM UI.</param>
		/// <param name="isHandled">Indicates whether the exception is handled or not.</param>
		void CaptureException(Exception exception, string culprit = null, bool isHandled = false);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
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
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="capturedAction">The <see cref="Action" /> that points to the code that you want to capture as a span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
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
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
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
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="func">The <see cref="Func{Task}" /> that points to the async code that you want to capture as a span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null);

		/// <summary>
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
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
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
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
		/// This is a convenient method which starts and ends a span on the given transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
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
		/// Ends the transaction and schedules it to be reported to the APM Server.
		/// It is illegal to call any methods on a span instance which has already ended.
		/// This also includes this method and <see cref="StartSpan" />.
		/// </summary>
		void End();

		/// <summary>
		/// Start and return a new custom span as a child of this transaction.
		/// </summary>
		/// <param name="name">The name of the span.</param>
		/// <param name="type">The type of the span.</param>
		/// <param name="subType">The subtype of the span.</param>
		/// <param name="action">The action of the span.</param>
		/// <returns>Returns the newly created and active span.</returns>
		ISpan StartSpan(string name, string type, string subType = null, string action = null);
	}
}
