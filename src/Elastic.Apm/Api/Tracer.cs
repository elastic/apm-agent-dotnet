// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private readonly IConfigurationSnapshotProvider _configurationProvider;
		private readonly IApmLogger _logger;
		private readonly IPayloadSender _sender;
		private readonly Service _service;
		private readonly BreakdownMetricsProvider _breakdownMetricsProvider;

		public Tracer(
			IApmLogger logger,
			Service service,
			IPayloadSender payloadSender,
			IConfigurationSnapshotProvider configurationProvider,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			IApmServerInfo apmServerInfo,
			BreakdownMetricsProvider breakdownMetricsProvider
		)
		{
			_logger = logger?.Scoped(nameof(Tracer));
			_service = service;
			_sender = payloadSender.ThrowIfArgumentNull(nameof(payloadSender));
			_configurationProvider = configurationProvider.ThrowIfArgumentNull(nameof(configurationProvider));
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
			bool ignoreActivity = false, IEnumerable<SpanLink> links = null
		)
		{
			if (_configurationProvider.CurrentSnapshot.Enabled && _configurationProvider.CurrentSnapshot.Recording)
				return StartTransactionInternal(name, type, distributedTracingData, ignoreActivity, links: links);

			return new NoopTransaction(name, type, CurrentExecutionSegmentsContainer, _configurationProvider.CurrentSnapshot);
		}

		internal Transaction StartTransactionInternal(string name, string type,
			long? timestamp = null, bool ignoreActivity = false, string id = null, string traceId = null,
			DistributedTracingData distributedTracingData = null,
			IEnumerable<SpanLink> links = null,
			Activity current = null
		)
			=> StartTransactionInternal(name, type, distributedTracingData, ignoreActivity, timestamp, id, traceId, links, current: current);

		private Transaction StartTransactionInternal(string name, string type, DistributedTracingData distributedTracingData = null,
			bool ignoreActivity = false, long? timestamp = null, string id = null, string traceId = null, IEnumerable<SpanLink> links = null,
			Activity current = null
		)
		{
			var currentConfig = _configurationProvider.CurrentSnapshot;
			var retVal = new Transaction(_logger, name, type, new Sampler(currentConfig.TransactionSampleRate), distributedTracingData
				, _sender, currentConfig, CurrentExecutionSegmentsContainer, _apmServerInfo, _breakdownMetricsProvider, ignoreActivity, timestamp, id,
				traceId: traceId, links: links, current: current)
			{ Service = _service };

			_logger?.Debug()?.Log("Starting {TransactionValue}", retVal);
			return retVal;
		}

		public void CaptureTransaction(string name, string type, Action<ITransaction> action, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);

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

		public void CaptureTransaction(string name, string type, Action action, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);

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

		public T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);
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

		public T CaptureTransaction<T>(string name, string type, Func<T> func, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);
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

		public async Task CaptureTransaction(string name, string type, Func<Task> func, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);
			try
			{
				await func();
			}
			catch (OperationCanceledException ex)
			{
				transaction.CaptureError("Task canceled", "A task was canceled", new StackTrace(ex).GetFrames());

				throw;
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}
		}

		public async Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null
		)
		{

			var transaction = StartTransaction(name, type, distributedTracingData, links: links);
			try
			{
				await func(transaction);
			}
			catch (OperationCanceledException ex)
			{
				transaction.CaptureError("Task canceled", "A task was canceled", new StackTrace(ex).GetFrames());

				throw;
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}
		}

		public async Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func, DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);
			try
			{
				return await func();
			}
			catch (OperationCanceledException ex)
			{
				transaction.CaptureError("Task canceled", "A task was canceled", new StackTrace(ex).GetFrames());

				throw;
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}
			return default;
		}

		public async Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func,
			DistributedTracingData distributedTracingData = null, IEnumerable<SpanLink> links = null)
		{
			var transaction = StartTransaction(name, type, distributedTracingData, links: links);
			try
			{
				return await func(transaction);
			}
			catch (OperationCanceledException ex)
			{
				transaction.CaptureError("Task canceled", "A task was canceled", new StackTrace(ex).GetFrames());

				throw;
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				transaction.End();
			}
			return default;
		}

		public void CaptureError(string message, string culprit, StackFrame[] frames = null, string parentId = null,
			Dictionary<string, Label> labels = null
		)
		{
			if (!_configurationProvider.CurrentSnapshot.Enabled || !_configurationProvider.CurrentSnapshot.Recording)
				return;

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
				_configurationProvider.CurrentSnapshot,
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
			if (!_configurationProvider.CurrentSnapshot.Enabled || !_configurationProvider.CurrentSnapshot.Recording)
				return;

			var currentTransaction = CurrentExecutionSegmentsContainer.CurrentTransaction;

			IExecutionSegment currentExecutionSegment = CurrentExecutionSegmentsContainer.CurrentSpan;
			currentExecutionSegment ??= currentTransaction;

			ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_sender,
				currentExecutionSegment,
				_configurationProvider.CurrentSnapshot,
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
			if (!_configurationProvider.CurrentSnapshot.Enabled || !_configurationProvider.CurrentSnapshot.Recording)
				return;

			var currentTransaction = CurrentExecutionSegmentsContainer.CurrentTransaction;

			IExecutionSegment currentExecutionSegment = CurrentExecutionSegmentsContainer.CurrentSpan;
			currentExecutionSegment ??= currentTransaction;

			ExecutionSegmentCommon.CaptureErrorLog(
				errorLog,
				_sender,
				_logger,
				currentExecutionSegment,
				_configurationProvider.CurrentSnapshot,
				currentTransaction as Transaction,
				null, //we don't pass specific parent id - it's either the current execution segments id, or null
				_apmServerInfo,
				exception,
				labels
			);
		}
	}
}
