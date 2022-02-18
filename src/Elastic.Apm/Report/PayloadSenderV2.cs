// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Cloud;
using Elastic.Apm.Config;
using Elastic.Apm.Filters;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Report
{
	/// <summary>
	/// Responsible for sending the data to APM server. Implements Intake V2.
	/// Each instance creates its own thread to do the work. Therefore, instances should be reused if possible.
	/// </summary>
	internal class PayloadSenderV2 : BackendCommComponentBase, IPayloadSender
	{
		private const string ThisClassName = nameof(PayloadSenderV2);
		internal readonly List<Func<IError, IError>> ErrorFilters = new List<Func<IError, IError>>();
		internal readonly List<Func<ISpan, ISpan>> SpanFilters = new List<Func<ISpan, ISpan>>();
		internal readonly List<Func<ITransaction, ITransaction>> TransactionFilters = new List<Func<ITransaction, ITransaction>>();

		private readonly IApmServerInfo _apmServerInfo;
		private readonly CloudMetadataProviderCollection _cloudMetadataProviderCollection;
		internal readonly Api.System System;
		private readonly IConfiguration _configuration;

		private readonly BatchBlock<object> _eventQueue;
		private readonly TimeSpan _flushInterval;
		private readonly Uri _intakeV2EventsAbsoluteUrl;
		private readonly IApmLogger _logger;
		private readonly int _maxQueueEventCount;
		private readonly Metadata _metadata;
		private readonly PayloadItemSerializer _payloadItemSerializer;

		private string _cachedMetadataJsonLine;
		private long _eventQueueCount;

		public PayloadSenderV2(
			IApmLogger logger,
			IConfiguration configuration,
			Service service,
			Api.System system,
			IApmServerInfo apmServerInfo,
			HttpMessageHandler httpMessageHandler = null,
			string dbgName = null,
			bool isEnabled = true,
			IEnvironmentVariables environmentVariables = null
		)
			: base(isEnabled, logger, ThisClassName, service, configuration, httpMessageHandler)
		{
			if (!isEnabled)
				return;

			_logger = logger?.Scoped(ThisClassName + (dbgName == null ? "" : $" (dbgName: `{dbgName}')"));
			_payloadItemSerializer = new PayloadItemSerializer();
			_configuration = configuration;

			_intakeV2EventsAbsoluteUrl = BackendCommUtils.ApmServerEndpoints.BuildIntakeV2EventsAbsoluteUrl(configuration.ServerUrl);

			System = system;

			_cloudMetadataProviderCollection = new CloudMetadataProviderCollection(configuration.CloudProvider, _logger, environmentVariables);
			_apmServerInfo = apmServerInfo;
			_metadata = new Metadata { Service = service, System = System };
			foreach (var globalLabelKeyValue in configuration.GlobalLabels) _metadata.Labels.Add(globalLabelKeyValue.Key, globalLabelKeyValue.Value);

			if (configuration.MaxQueueEventCount < configuration.MaxBatchEventCount)
			{
				_logger?.Error()
					?.Log(
						"MaxQueueEventCount is less than MaxBatchEventCount - using MaxBatchEventCount as MaxQueueEventCount."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " MaxBatchEventCount: {MaxBatchEventCount}.",
						configuration.MaxQueueEventCount, configuration.MaxBatchEventCount);

				_maxQueueEventCount = configuration.MaxBatchEventCount;
			}
			else
				_maxQueueEventCount = configuration.MaxQueueEventCount;

			_flushInterval = configuration.FlushInterval;

			_logger?.Debug()
				?.Log(
					"Using the following configuration options:"
					+ " Events intake API absolute URL: {EventsIntakeAbsoluteUrl}"
					+ ", FlushInterval: {FlushInterval}"
					+ ", MaxBatchEventCount: {MaxBatchEventCount}"
					+ ", MaxQueueEventCount: {MaxQueueEventCount}"
					, _intakeV2EventsAbsoluteUrl.Sanitize(), _flushInterval.ToHms(), configuration.MaxBatchEventCount, _maxQueueEventCount);

			_eventQueue = new BatchBlock<object>(configuration.MaxBatchEventCount);

			SetUpFilters(TransactionFilters, SpanFilters, ErrorFilters, apmServerInfo, logger);
			StartWorkLoop();
		}

		internal static void SetUpFilters(
			List<Func<ITransaction, ITransaction>> transactionFilters,
			List<Func<ISpan, ISpan>> spanFilters,
			List<Func<IError, IError>> errorFilters,
			IApmServerInfo apmServerInfo,
			IApmLogger logger)
		{
			transactionFilters.Add(new TransactionIgnoreUrlsFilter().Filter);
			transactionFilters.Add(new HeaderDictionarySanitizerFilter().Filter);
			// with this, stack trace demystification and conversion to the intake API model happens on a non-application thread:
			spanFilters.Add(new SpanStackTraceCapturingFilter(logger, apmServerInfo).Filter);
			errorFilters.Add(new ErrorContextSanitizerFilter().Filter);
		}

		private bool _getApmServerVersion;
		private bool _getCloudMetadata;
		private static readonly UTF8Encoding Utf8Encoding;
		private static readonly MediaTypeHeaderValue MediaTypeHeaderValue;

		static PayloadSenderV2()
		{
			Utf8Encoding = new UTF8Encoding(false);
			MediaTypeHeaderValue = new MediaTypeHeaderValue("application/x-ndjson")
			{
				CharSet = Utf8Encoding.WebName
			};
		}

		public void QueueTransaction(ITransaction transaction) => EnqueueEvent(transaction, "Transaction");

		public void QueueSpan(ISpan span) => EnqueueEvent(span, "Span");

		public void QueueMetrics(IMetricSet metricSet) => EnqueueEvent(metricSet, "MetricSet");

		public void QueueError(IError error) => EnqueueEvent(error, "Error");

		internal bool EnqueueEvent(object eventObj, string dbgEventKind)
		{
			ThrowIfDisposed();

			// Enforce _maxQueueEventCount manually instead of using BatchBlock's BoundedCapacity
			// because of the issue of Post returning false when TriggerBatch is in progress. For more details see
			// https://stackoverflow.com/questions/35626955/unexpected-behaviour-tpl-dataflow-batchblock-rejects-items-while-triggerbatch
			var newEventQueueCount = Interlocked.Increment(ref _eventQueueCount);
			if (newEventQueueCount > _maxQueueEventCount)
			{
				_logger.Debug()
					?.Log("Queue reached max capacity - " + dbgEventKind + " will be discarded."
						+ " newEventQueueCount: {EventQueueCount}."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " " + dbgEventKind + ": {" + dbgEventKind + "}."
						, newEventQueueCount, _maxQueueEventCount, eventObj);
				Interlocked.Decrement(ref _eventQueueCount);
				return false;
			}

			var enqueuedSuccessfully = _eventQueue.Post(eventObj);
			if (!enqueuedSuccessfully)
			{
				_logger.Debug()
					?.Log("Failed to enqueue " + dbgEventKind + "."
						+ " newEventQueueCount: {EventQueueCount}."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " " + dbgEventKind + ": {" + dbgEventKind + "}."
						, newEventQueueCount, _maxQueueEventCount, eventObj);
				Interlocked.Decrement(ref _eventQueueCount);
				return false;
			}

			_logger.Debug()
				?.Log("Enqueued " + dbgEventKind + "."
					+ " newEventQueueCount: {EventQueueCount}."
					+ " MaxQueueEventCount: {MaxQueueEventCount}."
					+ " " + dbgEventKind + ": {" + dbgEventKind + "}."
					, newEventQueueCount, _maxQueueEventCount, eventObj);

			if (_flushInterval == TimeSpan.Zero) _eventQueue.TriggerBatch();

			return true;
		}

		protected override async Task WorkLoopIteration()
		{
			if (!_getCloudMetadata)
			{
				var cloud = await _cloudMetadataProviderCollection.GetMetadataAsync().ConfigureAwait(false);
				if (cloud != null)
					_metadata.Cloud = cloud;

				_getCloudMetadata = true;
			}

			if (!_getApmServerVersion && _apmServerInfo?.Version is null)
			{
				await ApmServerInfoProvider.FillApmServerInfo(_apmServerInfo, _logger, _configuration, HttpClient).ConfigureAwait(false);
				_getApmServerVersion = true;
			}

			var batch = await ReceiveBatchAsync().ConfigureAwait(false);
			await ProcessQueueItems(batch).ConfigureAwait(false);
		}

		private async Task<object[]> ReceiveBatchAsync()
		{
			var receiveAsyncTask = _eventQueue.ReceiveAsync(CancellationTokenSource.Token);

			if (_flushInterval == TimeSpan.Zero)
				_logger.Trace()?.Log("Waiting for data to send... (not using FlushInterval timer because FlushInterval is 0)");
			else
			{
				_logger.Trace()?.Log("Waiting for data to send... FlushInterval: {FlushInterval}", _flushInterval.ToHms());
				while (true)
				{
					if (await TryAwaitOrTimeout(receiveAsyncTask, _flushInterval, CancellationTokenSource.Token).ConfigureAwait(false))
						break;

					_eventQueue.TriggerBatch();
				}
			}

			var eventBatchToSend = await receiveAsyncTask.ConfigureAwait(false);
			var newEventQueueCount = Interlocked.Add(ref _eventQueueCount, -eventBatchToSend.Length);
			_logger.Trace()
				?.Log("There's data to be sent. Batch size: {BatchSize}. newEventQueueCount: {newEventQueueCount}. First event: {Event}."
					, eventBatchToSend.Length, newEventQueueCount, eventBatchToSend.Length > 0 ? eventBatchToSend[0].ToString() : "<N/A>");
			return eventBatchToSend;
		}

		/// <summary>
		/// It's recommended to use this method (or another TryAwaitOrTimeout or AwaitOrTimeout method)
		/// instead of just Task.WhenAny(taskToAwait, Task.Delay(timeout))
		/// because this method cancels the timer for timeout while <c>Task.Delay(timeout)</c>.
		/// If the number of “zombie” timer jobs starts becoming significant, performance could suffer.
		/// For more detailed explanation see https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
		/// </summary>
		/// <returns><c>true</c> if <c>taskToAwait</c> completed before the timeout, <c>false</c> otherwise</returns>
		private static async Task<bool> TryAwaitOrTimeout(Task taskToAwait, TimeSpan timeout, CancellationToken cancellationToken = default)
		{
			var timeoutDelayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var timeoutDelayTask = Task.Delay(timeout, timeoutDelayCts.Token);
			try
			{
				var completedTask = await Task.WhenAny(taskToAwait, timeoutDelayTask).ConfigureAwait(false);
				if (completedTask == taskToAwait)
				{
					await taskToAwait.ConfigureAwait(false);
					return true;
				}

				Assertion.IfEnabled?.That(completedTask == timeoutDelayTask
					, $"{nameof(completedTask)}: {completedTask}, {nameof(timeoutDelayTask)}: timeOutTask, {nameof(taskToAwait)}: taskToAwait");
				// no need to cancel timeout timer if it has been triggered
				timeoutDelayTask = null;
				return false;
			}
			finally
			{
				if (timeoutDelayTask != null) timeoutDelayCts.Cancel();
				timeoutDelayCts.Dispose();
			}
		}

		private async Task ProcessQueueItems(object[] queueItems)
		{
			// can reuse underlying buffers from a pool in future.
			using var stream = new MemoryStream(1024);

			try
			{
				_cachedMetadataJsonLine ??= _payloadItemSerializer.Serialize(_metadata);

				using (var writer = new StreamWriter(stream, Utf8Encoding, 1024, true))
				{
					writer.Write("{\"metadata\":");
					writer.Write(_cachedMetadataJsonLine);
					writer.Write("}\n");

					foreach (var item in queueItems)
					{
						switch (item)
						{
							case Transaction transaction:
								if (TryExecuteFilter(TransactionFilters, transaction) != null) Serialize(item, "transaction", writer);
								break;
							case Span span:
								if (TryExecuteFilter(SpanFilters, span) != null) Serialize(item, "span", writer);
								break;
							case Error error:
								if (TryExecuteFilter(ErrorFilters, error) != null) Serialize(item, "error", writer);
								break;
							case MetricSet _:
								Serialize(item, "metricset", writer);
								break;
						}
					}
				}

				stream.Position = 0;
				using (var content = new StreamContent(stream))
				{
					content.Headers.ContentType = MediaTypeHeaderValue;
					var response = await HttpClient.PostAsync(_intakeV2EventsAbsoluteUrl, content, CancellationTokenSource.Token)
						.ConfigureAwait(false);

					if (response is null || !response.IsSuccessStatusCode)
					{
						_logger?.Error()
							?.Log("Failed sending event."
								+ " Events intake API absolute URL: {EventsIntakeAbsoluteUrl}."
								+ " APM Server response: status code: {ApmServerResponseStatusCode}"
								+ ", content: \n{ApmServerResponseContent}"
								, _intakeV2EventsAbsoluteUrl.Sanitize()
								, response?.StatusCode,
								response is null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
					}
					else
					{
						_logger?.Debug()
							?.Log("Sent items to server:"
								+ $"{Environment.NewLine}{TextUtils.Indentation}{{SerializedItems}}",
								string.Join($",{Environment.NewLine}{TextUtils.Indentation}", queueItems));
					}
				}
			}
			catch (OperationCanceledException)
			{
				// handle cancellation specifically
				_logger?.Warning()
					?.Log(
						"Cancellation requested. Following events were not transferred successfully to the server ({ApmServerUrl}):"
							+ $"{Environment.NewLine}{TextUtils.Indentation}{{SerializedItems}}"
						, HttpClient.BaseAddress.Sanitize()
						, string.Join($",{Environment.NewLine}{TextUtils.Indentation}", queueItems));

				// throw to allow Workloop to handle
				if(CancellationTokenSource.IsCancellationRequested)
				{
					throw;
				}
			}
			catch (Exception e)
			{
				_logger?.Warning()
					?.LogException(
						e,
						"Failed sending events. Following events were not transferred successfully to the server ({ApmServerUrl}):"
							+ $"{Environment.NewLine}{TextUtils.Indentation}{{SerializedItems}}"
						, HttpClient.BaseAddress.Sanitize()
						, string.Join($",{Environment.NewLine}{TextUtils.Indentation}", queueItems)
					);
			}
		}

		private void Serialize(object item, string eventType, TextWriter writer)
		{
			writer.Write("{\"");
			writer.Write(eventType);
			writer.Write("\":");
			var traceLogger = _logger?.IfLevel(LogLevel.Trace);
			if (traceLogger.HasValue)
			{
				var serialized = _payloadItemSerializer.Serialize(item);
				writer.Write(serialized);
				traceLogger.Value.Log("Serialized item to send: {ItemToSend} as {SerializedItem}", item, serialized);
			}
			else
				_payloadItemSerializer.Serialize(item, writer);

			writer.Write("}\n");
		}

		// Executes filters for the given filter collection and handles return value and errors
		// ReSharper disable once SuggestBaseTypeForParameter
		// internal code, no need to use base type for parameter as ReSharper suggests
		private T TryExecuteFilter<T>(List<Func<T, T>> filters, T item) where T : class
		{
			if (filters.Count == 0)
				return item;

			foreach (var filter in filters)
			{
				try
				{
					_logger?.Trace()?.Log("Start executing filter on transaction");
					var itemAfterFilter = filter(item);
					if (itemAfterFilter != null)
					{
						item = itemAfterFilter;
						continue;
					}

					_logger?.Debug()?.Log("Filter returns false, item won't be sent, {filteredItem}", item);
					return null;
				}
				catch (Exception e)
				{
					_logger.Warning()?.LogException(e, "Exception during execution of the filter on transaction");
				}
			}

			return item;
		}
	}
}
