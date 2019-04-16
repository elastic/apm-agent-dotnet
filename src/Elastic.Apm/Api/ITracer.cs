using System;
using System.Threading.Tasks;

namespace Elastic.Apm.Api
{
	public interface ITracer
	{
		/// <summary>
		/// Returns the currently active transaction.
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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		void CaptureTransaction(string name, string type, Action<ITransaction> action, (string traceId, string parentId) traceContext = default);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="action">
		/// The <see cref="Action{ITransaction}" /> that points to the code that you want to capture as a
		/// transaction.</param>
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		void CaptureTransaction(string name, string type, Action action, (string traceId, string parentId) traceContext = default);

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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		/// <returns>
		/// The result of the
		/// <param name="func"></param>
		/// .
		/// </returns>
		T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func, (string traceId, string parentId) traceContext = default);

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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		T CaptureTransaction<T>(string name, string type, Func<T> func, (string traceId, string parentId) traceContext = default);

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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureTransaction(string name, string type, Func<Task> func, (string traceId, string parentId) traceContext = default);

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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func, (string traceId, string parentId) traceContext = default);

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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
		Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func, (string traceId, string parentId) traceContext = default);

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
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
		Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func, (string traceId, string parentId) traceContext = default);

		/// <summary>
		/// Starts and returns a custom transaction.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="traceContext">In case of a distributed trace, you can pass a the Trace Context to the API. By doing so, the new transaction will be
		/// automatically part of a distributed trace. The Trace Context encapsulates a trace id, which is the id of the whole distributed trace, and a
		/// parent id, which is the id of the span or the transaction that initiated the new transaction. Both values must be present.</param>
		/// <returns>The transaction that is created based on the parameters. This transaction is already active.</returns>
		ITransaction StartTransaction(string name, string type, (string traceId, string parentId) traceContext = default);
	}
}
