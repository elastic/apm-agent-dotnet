﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.DistributedTracing;
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
		private readonly Sampler _sampler;

		public Tracer(IApmLogger logger, Service service, IPayloadSender payloadSender, IConfigurationReader configurationReader)
		{
			_logger = logger?.Scoped(nameof(Tracer));
			_service = service;
			_sender = payloadSender;
			_sampler = new Sampler(1.0);
		}

		public ITransaction CurrentTransaction => Agent.TransactionContainer.Transactions.Value;

		public ITransaction StartTransaction(string name, string type, (string traceId, string parentId) traceContext = default)
		{
			var (traceId, parentId) = traceContext;
			if (TraceParent.IsTraceIdValid(traceId) && TraceParent.IsTraceParentValid(parentId))
			{
				return  StartTransactionInternal(name, type, traceId, parentId);
			}
			return StartTransactionInternal(name, type);
		}

		internal Transaction StartTransactionInternal(string name, string type, string traceId = null, string parentId = null)
		{
			var retVal = new Transaction(_logger, name, type, _sender, traceId, parentId)
			{
				Name = name,
				Type = type,
				Service = _service
			};

			Agent.TransactionContainer.Transactions.Value = retVal;
			_logger.Debug()?.Log("Starting {TransactionValue}", retVal);
			return retVal;
		}

		public void CaptureTransaction(string name, string type, Action<ITransaction> action, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);

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

		public void CaptureTransaction(string name, string type, Action action, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);

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

		public T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);
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

		public T CaptureTransaction<T>(string name, string type, Func<T> func, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);
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

		public Task CaptureTransaction(string name, string type, Func<Task> func, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);
			var task = func();
			RegisterContinuation(task, transaction);
			return task;
		}

		public Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);
			var task = func(transaction);
			RegisterContinuation(task, transaction);
			return task;
		}

		public Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);
			var task = func();
			RegisterContinuation(task, transaction);

			return task;
		}

		public Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func, (string traceId, string parentId) traceContext = default)
		{
			var transaction = StartTransaction(name, type, traceContext);
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
