using System;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
	public interface ITracer
	{
		/// <summary>
		/// Returns the currently active transaction.
		/// </summary>
		ITransaction CurrentTransaction { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		Service Service { get; set; }

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
		void CaptureTransaction(string name, string type, Action<ITransaction> action);

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
		void CaptureTransaction(string name, string type, Action action);

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
		/// <returns>The result of the
		/// <param name="func"></param>
		/// .
		/// </returns>
		T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func);

		/// <summary>
		/// This is a convenient method which starts and ends a transaction and captures unhandled exceptions
		/// and schedules it to be reported to the APM Server.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <param name="func">
		/// The <see cref="Action{ITransaction}" /> that points to the code that you want to capture as a
		/// transaction.
		/// </param>
		/// <typeparam name="T">The return type of the code that you want to capture as transaction.</typeparam>
		/// <returns>
		/// The result of the
		/// <param name="func"></param>
		/// .
		/// </returns>
		T CaptureTransaction<T>(string name, string type, Func<T> func);

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
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureTransaction(string name, string type, Func<Task> func);

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
		/// <returns>The <see cref="Task" /> that you can await on.</returns>
		Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func);

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
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
		Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func);

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
		/// <returns>The <see cref="Task{T}" /> that you can await on.</returns>
		Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func);

		/// <summary>
		/// Starts and returns a custom transaction.
		/// </summary>
		/// <param name="name">The name of the transaction.</param>
		/// <param name="type">The type of the transaction.</param>
		/// <returns></returns>
		ITransaction StartTransaction(string name, string type);
	}
}
