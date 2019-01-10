using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
	internal class Tracer : ITracer
	{
		private static AbstractLogger _publicTracerLogger;

		public ITransaction CurrentTransaction
			=> TransactionContainer.Transactions.Value;

		public static AbstractLogger PublicTracerLogger => _publicTracerLogger ?? (_publicTracerLogger = Agent.CreateLogger("AgentAPI"));

		private Service _service;

		/// <inheritdoc />
		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service
		{
			get => _service ?? (_service = new Service
			{
				Name = Assembly.GetEntryAssembly()?.GetName().Name,
				Agent = new Service.AgentC
				{
					Name = Consts.AgentName,
					Version = Consts.AgentVersion
				}
			});
			set => _service = value;
		}

		public ITransaction StartTransaction(string name, string type)
		{
			var retVal = new Transaction(name, type)
			{
				Name = name,
				Type = type,
				Service = Service
			};

			TransactionContainer.Transactions.Value = retVal;
			return retVal;
		}

		public void CaptureTransaction(string name, string type, Action<ITransaction> action)
		{
			var transaction = StartTransaction(name, type);

			try
			{
				action(transaction);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}
		}

		public void CaptureTransaction(string name, string type, Action action)
		{
			var transaction = StartTransaction(name, type);

			try
			{
				action();
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}
		}

		public T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func)
		{
			var transaction = StartTransaction(name, type);
			var retVal = default(T);
			try
			{
				retVal = func(transaction);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}

			return retVal;
		}

		public T CaptureTransaction<T>(string name, string type, Func<T> func)
		{
			var transaction = StartTransaction(name, type);
			var retVal = default(T);
			try
			{
				retVal = func();
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}

			return retVal;
		}

		public Task CaptureTransaction(string name, string type, Func<Task> func)
		{
			var transaction = StartTransaction(name, type);
			var task = func();
			RegisterContinuation(task, transaction);
			return task;
		}

		public Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func)
		{
			var transaction = StartTransaction(name, type);
			var task = func(transaction);
			RegisterContinuation(task, transaction);
			return task;
		}

		public Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func)
		{
			var transaction = StartTransaction(name, type);
			var task = func();
			RegisterContinuation(task, transaction);

			return task;
		}

		public Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func)
		{
			var transaction = StartTransaction(name, type);
			var task = func(transaction);
			RegisterContinuation(task, transaction);
			return task;
		}

		/// <summary>
		/// Registers a continuation on the task.
		/// Within the continuation it ends the transaction and captures errors
		/// </summary>
		private static void RegisterContinuation(Task task, ITransaction transaction) => task.ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				if (t.Exception != null)
				{
					if (t.Exception is AggregateException aggregateException)
					{
						ExceptionFilter.Capture(
							aggregateException.InnerExceptions.Count == 1
								? aggregateException.InnerExceptions[0]
								: aggregateException.Flatten(), transaction);
					}
					else
						ExceptionFilter.Capture(t.Exception, transaction);
				}
				else
					transaction.CaptureError("Task faulted", "A task faulted", new StackTrace().GetFrames());
			}
			else if (t.IsCanceled)
			{
				if (t.Exception == null)
				{
					transaction.CaptureError("Task canceled", "A task was canceled",
						new StackTrace().GetFrames()); //TODO: this async stacktrace is hard to use, make it readable!
				}
				else
					transaction.CaptureException(t.Exception);
			}

			transaction.End();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}
}
