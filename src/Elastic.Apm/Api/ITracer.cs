// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Elastic.Apm.Api
{
	public interface ITracer
	{
		/// <summary>
		/// Gets the currently active span.
		/// Returns <c>null</c> if there's no currently active span.
		/// </summary>
		ISpan CurrentSpan { get; }

		/// <summary>
		/// Gets the currently active transaction.
		/// Returns <c>null</c> if there's no currently active transaction.
		/// </summary>
		ITransaction CurrentTransaction { get; }

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="action">
		/// The <see cref="Action{ITransaction}" /> that points to the code that you want to capture as a transaction.
		/// The <see cref="ITransaction" /> parameter gives you access to the transaction which is created by this method.
		/// </param>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		void CaptureTransaction(string name, string type, Action<ITransaction> action, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="action">
		/// The <see cref="Action{ITransaction}" /> that points to the code that you want to capture as a
		/// transaction.
		/// </param>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		void CaptureTransaction(string name, string type, Action action, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Action{ITransaction}" /> that points to the code that you want to capture as a transaction.
		/// The <see cref="ITransaction" /> parameter gives you access to the transaction which is created by this method.
		/// </param>
		/// <typeparam name="T">The return type of the code that you want to capture as transaction.</typeparam>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>
		/// The result of the <paramref name="func" />.
		/// </returns>
		T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Func{T}" /> that points to the code that you want to capture as a
		/// transaction.
		/// </param>
		/// <typeparam name="T">The return type of the code that you want to capture as transaction.</typeparam>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>
		/// The result of the <paramref name="func" />.
		/// </returns>
		T CaptureTransaction<T>(string name, string type, Func<T> func, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Func{Task}" /> that points to the async code that you want to capture as a
		/// transaction.
		/// </param>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>A <see cref="Task" /> that can be awaited</returns>
		Task CaptureTransaction(string name, string type, Func<Task> func, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Func{Task}" /> that points to the async code that you want to capture as a transaction.
		/// The <see cref="ITransaction" /> parameter gives you access to the transaction which is created by this method.
		/// </param>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>A <see cref="Task" /> that can be awaited</returns>
		Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Func{Task}" /> that points to the async code with a return value that you want to
		/// capture as a transaction.
		/// </param>
		/// <typeparam name="T">The return type of the <see cref="Task{T}" /> that you want to capture as transaction.</typeparam>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>A <see cref="Task{T}" /> that can be awaited</returns>
		Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func, DistributedTracingData distributedTracingData = null);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Func{Task}" /> that points to the async code with a return value that you want to capture as a
		/// transaction.
		/// The <see cref="ITransaction" /> parameter gives you access to the transaction which is created by this method.
		/// </param>
		/// <typeparam name="T">The return type of the <see cref="Task{T}" /> that you want to capture as transaction.</typeparam>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>A <see cref="Task{T}" /> that can be awaited.</returns>
		Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func, DistributedTracingData distributedTracingData = null
		);

		/// <summary>
		/// Starts and returns a custom transaction.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="IExecutionSegment.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <param name="ignoreActivity">
		/// The agent by default does a best effort to keep <see cref="Activity.TraceId"/> in sync with the trace id used in Elastic APM.
		/// By setting <paramref name="ignoreActivity"/> to <c>false</c> you can turn off this functionality. </param>
		/// <returns>The transaction that is created based on the parameters. This transaction is already active.</returns>
		ITransaction StartTransaction(string name, string type, DistributedTracingData distributedTracingData = null, bool ignoreActivity = false);

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
		void CaptureError(string message, string culprit, StackFrame[] frames = null, string parentId = null);

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
	}
}
