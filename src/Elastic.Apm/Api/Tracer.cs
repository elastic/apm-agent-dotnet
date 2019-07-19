using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Report;

namespace Elastic.Apm.Api
{
	internal class Tracer : ITracer
	{
		private readonly ScopedLogger _logger;
		private readonly IPayloadSender _sender;
		private readonly Service _service;

		public Tracer(
			IApmLogger logger,
			Service service,
			IPayloadSender payloadSender,
			IConfigurationReader configurationReader,
			Sampler sampler,
			ICurrentExecutionSegmentHolder currentExecutionSegmentHolder)
		{
			_logger = logger?.Scoped(nameof(Tracer));
			_service = service;
			_sender = payloadSender.ThrowIfArgumentNull(nameof(payloadSender));
			Sampler = sampler.ThrowIfArgumentNull(nameof(sampler));
			CurrentExecutionSegmentHolder = currentExecutionSegmentHolder.ThrowIfArgumentNull(nameof(currentExecutionSegmentHolder));
		}

		public ITransaction CurrentTransaction => CurrentExecutionSegmentHolder.CurrentTransactionInternal;
//		public ISpan CurrentSpan => _currentExecutionSegmentHolder.CurrentSpanInternal;

		internal Sampler Sampler { get; set; }

		internal ICurrentExecutionSegmentHolder CurrentExecutionSegmentHolder { get; }

		public ITransaction StartTransaction(string name, string type, DistributedTracingData distributedTracingData = null) =>
			StartTransactionInternal(name, type, distributedTracingData);

		internal Transaction StartTransactionInternal(string name, string type, DistributedTracingData distributedTracingData = null)
		{
			var retVal = new Transaction(_logger, CurrentExecutionSegmentHolder, name, type, Sampler, distributedTracingData, _sender) { Service = _service };

			_logger.Debug()?.Log("Starting {TransactionValue}", retVal);
			return retVal;
		}

		public void CaptureTransaction(string name, string type, Action<ITransaction> action, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);

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

		public void CaptureTransaction(string name, string type, Action action, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);

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

		public T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);
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

		public T CaptureTransaction<T>(string name, string type, Func<T> func, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);
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

		public Task CaptureTransaction(string name, string type, Func<Task> func, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);
			var task = func();
			RegisterContinuation(task, transaction);
			return task;
		}

		public Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);
			var task = func(transaction);
			RegisterContinuation(task, transaction);
			return task;
		}

		public Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func, DistributedTracingData distributedTracingData = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);
			var task = func();
			RegisterContinuation(task, transaction);

			return task;
		}

		public Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func,
			DistributedTracingData distributedTracingData = null
		)
		{
			var transaction = StartTransaction(name, type, distributedTracingData);
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
					transaction.CaptureError("Task faulted", "A task faulted", new StackTrace(true).GetFrames());
			}
			else if (t.IsCanceled)
			{
				if (t.Exception == null)
				{
					transaction.CaptureError("Task canceled", "A task was canceled",
						new StackTrace(true).GetFrames()); //TODO: this async stacktrace is hard to use, make it readable!
				}
				else
					transaction.CaptureException(t.Exception);
			}

			transaction.End();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}
}
