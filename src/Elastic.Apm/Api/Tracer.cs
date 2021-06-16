// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Api
{
	internal class Tracer : ITracer
	{
		private readonly IApmServerInfo _apmServerInfo;
		private readonly IConfigSnapshotProvider _configProvider;
		private readonly ScopedLogger _logger;
		private readonly IPayloadSender _sender;
		private readonly Service _service;
		private readonly BreakdownMetricsProvider _breakdownMetricsProvider;

		public Tracer(
			IApmLogger logger,
			Service service,
			IPayloadSender payloadSender,
			IConfigSnapshotProvider configProvider,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			IApmServerInfo apmServerInfo,
			BreakdownMetricsProvider breakdownMetricsProvider
		)
		{
			_logger = logger?.Scoped(nameof(Tracer));
			_service = service;
			_sender = payloadSender.ThrowIfArgumentNull(nameof(payloadSender));
			_configProvider = configProvider.ThrowIfArgumentNull(nameof(configProvider));
			CurrentExecutionSegmentsContainer = currentExecutionSegmentsContainer.ThrowIfArgumentNull(nameof(currentExecutionSegmentsContainer));
			DbSpanCommon = new DbSpanCommon(logger);
			_apmServerInfo = apmServerInfo;
			_breakdownMetricsProvider = breakdownMetricsProvider;
		}

		internal ICurrentExecutionSegmentsContainer CurrentExecutionSegmentsContainer { get; }

		public ISpan CurrentSpan => CurrentExecutionSegmentsContainer.CurrentSpan;

		public ITransaction CurrentTransaction => CurrentExecutionSegmentsContainer.CurrentTransaction;

		public DbSpanCommon DbSpanCommon { get; }

		public ITransaction StartTransaction(string name, string type, DistributedTracingData distributedTracingData = null,
			bool ignoreActivity = false
		)
		{
			if (_configProvider.CurrentSnapshot.Enabled && _configProvider.CurrentSnapshot.Recording)
				return StartTransactionInternal(name, type, distributedTracingData, ignoreActivity);

			return new NoopTransaction(name, type, CurrentExecutionSegmentsContainer);
		}

		internal Transaction StartTransactionInternal(string name, string type,
			long? timestamp = null
		)
			=> StartTransactionInternal(name, type, null, false, timestamp);

		private Transaction StartTransactionInternal(string name, string type, DistributedTracingData distributedTracingData = null,
			bool ignoreActivity = false, long? timestamp = null
		)
		{
			var currentConfig = _configProvider.CurrentSnapshot;
			var retVal = new Transaction(_logger, name, type, new Sampler(currentConfig.TransactionSampleRate), distributedTracingData
				, _sender, currentConfig, CurrentExecutionSegmentsContainer, _apmServerInfo, _breakdownMetricsProvider, ignoreActivity, timestamp)
			{
				Service = _service
			};



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
					if (t.Exception is { } aggregateException)
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

		public void CaptureError(string message, string culprit, StackFrame[] frames = null, string parentId = null,
			Dictionary<string, Label> labels = null
		)
		{
			var currentTransaction = CurrentExecutionSegmentsContainer.CurrentTransaction;

			IExecutionSegment currentExecutionSegment = CurrentExecutionSegmentsContainer.CurrentSpan;
			currentExecutionSegment ??= currentTransaction;

			ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_sender,
				_logger,
				currentExecutionSegment,
				_configProvider.CurrentSnapshot,
				currentTransaction as Transaction,
				_apmServerInfo,
				parentId,
				labels
			);
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null,
			Dictionary<string, Label> labels = default
		)
		{
			var currentTransaction = CurrentExecutionSegmentsContainer.CurrentTransaction;

			IExecutionSegment currentExecutionSegment = CurrentExecutionSegmentsContainer.CurrentSpan;
			currentExecutionSegment ??= currentTransaction;

			ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_sender,
				currentExecutionSegment,
				_configProvider.CurrentSnapshot,
				currentTransaction as Transaction,
				_apmServerInfo,
				culprit,
				isHandled,
				parentId,
				labels
			);
		}

		public void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null, Dictionary<string, Label> labels = null)
		{
			var currentTransaction = CurrentExecutionSegmentsContainer.CurrentTransaction;

			IExecutionSegment currentExecutionSegment = CurrentExecutionSegmentsContainer.CurrentSpan;
			currentExecutionSegment ??= currentTransaction;

			ExecutionSegmentCommon.CaptureErrorLog(
				errorLog,
				_sender,
				_logger,
				currentExecutionSegment,
				_configProvider.CurrentSnapshot,
				currentTransaction as Transaction,
				null, //we don't pass specific parent id - it's either the current execution segments id, or null
				_apmServerInfo,
				exception,
				labels
			);
		}
	}
}
