using System;
using System.Threading.Tasks;

namespace Elastic.Apm.Api
{
	public interface ITracer
	{
		/// <summary>
		/// Returns the currently active span.
		/// Returns <c>null</c> if there's no currently active span.
		/// </summary>
		ISpan CurrentSpan { get; }

		/// <summary>
		/// Returns the currently active transaction.
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>
		/// The result of the
		/// <param name="func"></param>
		/// .
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
		/// <returns>
		/// The result of the
		/// <param name="func"></param>
		/// .
		/// </returns>
		/// <param name="distributedTracingData">
		/// In case of a distributed trace, you can pass distributed tracing data to the API. By doing so, the new transaction will
		/// be
		/// automatically part of a distributed trace.
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
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
		/// Use <see cref="ISpan.OutgoingDistributedTracingData" /> to obtain distributed tracing data on the caller side.
		/// </param>
		/// <returns>The transaction that is created based on the parameters. This transaction is already active.</returns>
		ITransaction StartTransaction(string name, string type, DistributedTracingData distributedTracingData = null);
	}
}
