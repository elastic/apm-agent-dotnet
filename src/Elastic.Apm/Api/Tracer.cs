using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Api
{
	internal class Tracer : ITracer
	{
		public ITransaction CurrentTransaction => Agent.TransactionContainer.Transactions.Value;

		private readonly AbstractLogger _logger;
		private readonly Service _service;
		private readonly IPayloadSender _sender;

		public Tracer(AbstractLogger logger, Service service, IPayloadSender payloadSender)
		{
			_logger = logger;
			_service = service;
			_sender = payloadSender;
		}

		public ITransaction StartTransaction(string name, string type)
			=> StartTransactionInternal(name, type);

		internal Transaction StartTransactionInternal(string name, string type)
		{
			var retVal = new Transaction(_logger, name, type, _sender)
			{
				Name = name,
				Type = type,
				Service = _service
			};

			Agent.TransactionContainer.Transactions.Value = retVal;
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
