using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Report
{
	/// <summary>
	/// Responsible for sending the data to the server. Implements Intake V2.
	/// Each instance creates its own thread to do the work. Therefore, instances should be reused if possible.
	/// </summary>
	internal class PayloadSenderV2 : BackendCommComponentBase, IPayloadSender
	{
		private const string ThisClassName = nameof(PayloadSenderV2);

		internal readonly Api.System System;

		private readonly BatchBlock<object> _eventQueue;

		private readonly TimeSpan _flushInterval;
		private readonly Uri _intakeV2EventsAbsoluteUrl;

		private readonly IApmLogger _logger;
		private readonly int _maxQueueEventCount;
		private readonly Metadata _metadata;

		private readonly PayloadItemSerializer _payloadItemSerializer;

		internal readonly List<Func<ITransaction, bool>> TransactionFilters = new List<Func<ITransaction, bool>>();
		internal readonly List<Func<ISpan, bool>> SpanFilters = new List<Func<ISpan, bool>>();
		internal readonly List<Func<IError, bool>> ErrorFilters = new List<Func<IError, bool>>();

		public PayloadSenderV2(IApmLogger logger, IConfigSnapshot config, Service service, Api.System system,
			HttpMessageHandler httpMessageHandler = null, string dbgName = null
		)
			: base( /* isEnabled: */ true, logger, ThisClassName, service, config, httpMessageHandler)
		{
			_logger = logger?.Scoped(ThisClassName + (dbgName == null ? "" : $" (dbgName: `{dbgName}')"));
			_payloadItemSerializer = new PayloadItemSerializer(config);

			_intakeV2EventsAbsoluteUrl = BackendCommUtils.ApmServerEndpoints.BuildIntakeV2EventsAbsoluteUrl(config.ServerUrls.First());

			System = system;

			_metadata = new Metadata { Service = service, System = System };
			foreach (var globalLabelKeyValue in config.GlobalLabels) _metadata.Labels.Add(globalLabelKeyValue.Key, globalLabelKeyValue.Value);

			if (config.MaxQueueEventCount < config.MaxBatchEventCount)
			{
				_logger?.Error()
					?.Log(
						"MaxQueueEventCount is less than MaxBatchEventCount - using MaxBatchEventCount as MaxQueueEventCount."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " MaxBatchEventCount: {MaxBatchEventCount}.",
						config.MaxQueueEventCount, config.MaxBatchEventCount);

				_maxQueueEventCount = config.MaxBatchEventCount;
			}
			else
				_maxQueueEventCount = config.MaxQueueEventCount;

			_flushInterval = config.FlushInterval;

			_logger?.Debug()
				?.Log(
					"Using the following configuration options:"
					+ " Events intake API absolute URL: {EventsIntakeAbsoluteUrl}"
					+ ", FlushInterval: {FlushInterval}"
					+ ", MaxBatchEventCount: {MaxBatchEventCount}"
					+ ", MaxQueueEventCount: {MaxQueueEventCount}"
					, _intakeV2EventsAbsoluteUrl, _flushInterval.ToHms(), config.MaxBatchEventCount, _maxQueueEventCount);

			_eventQueue = new BatchBlock<object>(config.MaxBatchEventCount);

			StartWorkLoop();
		}

		private string _cachedMetadataJsonLine;

		private long _eventQueueCount;

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

		protected override async Task WorkLoopIteration() => await ProcessQueueItems(await ReceiveBatchAsync());

		private async Task<object[]> ReceiveBatchAsync()
		{
			var receiveAsyncTask = _eventQueue.ReceiveAsync(CtsInstance.Token);

			if (_flushInterval == TimeSpan.Zero)
				_logger.Trace()?.Log("Waiting for data to send... (not using FlushInterval timer because FlushInterval is 0)");
			else
			{
				_logger.Trace()?.Log("Waiting for data to send... FlushInterval: {FlushInterval}", _flushInterval.ToHms());
				while (true)
				{
					if (await TryAwaitOrTimeout(receiveAsyncTask, _flushInterval, CtsInstance.Token)) break;

					_eventQueue.TriggerBatch();
				}
			}

			var eventBatchToSend = await receiveAsyncTask;
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
				var completedTask = await Task.WhenAny(taskToAwait, timeoutDelayTask);
				if (completedTask == taskToAwait)
				{
					await taskToAwait;
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
			try
			{
				var ndjson = new StringBuilder();
				if (_cachedMetadataJsonLine == null)
					_cachedMetadataJsonLine = "{\"metadata\": " + _payloadItemSerializer.SerializeObject(_metadata) + "}";
				ndjson.AppendLine(_cachedMetadataJsonLine);

				foreach (var item in queueItems)
				{
					switch (item)
					{
						case Transaction transaction:
							if (TryExecuteFilter(TransactionFilters, transaction)) SerializeAndSend(item, "transaction");
							break;
						case Span span:
							if (TryExecuteFilter(SpanFilters, span)) SerializeAndSend(item, "span");
							break;
						case Error error:
							if (TryExecuteFilter(ErrorFilters, error)) SerializeAndSend(item, "error");
							break;
						case MetricSet _:
							SerializeAndSend(item, "metricset");
							break;
					}
				}

				var content = new StringContent(ndjson.ToString(), Encoding.UTF8, "application/x-ndjson");

				var result = await HttpClientInstance.PostAsync(_intakeV2EventsAbsoluteUrl, content, CtsInstance.Token);

				if (result != null && !result.IsSuccessStatusCode)
				{
					_logger?.Error()
						?.Log("Failed sending event."
							+ " Events intake API absolute URL: {EventsIntakeAbsoluteUrl}."
							+ " APM Server response: status code: {ApmServerResponseStatusCode}"
							+ ", content: \n{ApmServerResponseContent}"
							, _intakeV2EventsAbsoluteUrl, result.StatusCode, await result.Content.ReadAsStringAsync());
				}
				else
				{
					_logger?.Debug()
						?.Log("Sent items to server:\n{SerializedItems}",
							TextUtils.Indent(string.Join($",{Environment.NewLine}", queueItems.ToArray())));
				}

				void SerializeAndSend(object item, string eventType)
				{
					var serialized = _payloadItemSerializer.SerializeObject(item);
					ndjson.AppendLine($"{{\"{eventType}\": " + serialized + "}");
					_logger?.Trace()?.Log("Serialized item to send: {ItemToSend} as {SerializedItem}", item, serialized);
				}
			}
			catch (Exception e)
			{
				_logger?.Warning()
					?.LogException(
						e,
						"Failed sending events. Following events were not transferred successfully to the server ({ApmServerUrl}):\n{SerializedItems}"
						, HttpClientInstance.BaseAddress
						, TextUtils.Indent(string.Join($",{Environment.NewLine}", queueItems.ToArray()))
					);
			}

			// Executes filters for the given filter collection and handles return value and errors
			bool TryExecuteFilter<T>(IEnumerable<Func<T, bool>> filters, T item)
			{
				var sendEvent = true;
				var enumerable = filters as Func<T, bool>[] ?? filters.ToArray();
				if (!enumerable.Any()) return true;

				foreach (var filter in enumerable)
				{
					try
					{
						_logger?.Trace()?.Log("Start executing filter on transaction");
						sendEvent = filter(item);
						if (sendEvent) continue;

						_logger?.Debug()?.Log("Filter returns false, item won't be sent, {filteredItemm}", item);
						break;
					}
					catch (Exception e)
					{
						_logger.Warning()?.LogException(e, "Exception during execution of the filter on transaction");
					}
				}

				return sendEvent;
			}
		}
	}

	internal class Metadata
	{
		[JsonConverter(typeof(LabelsJsonConverter))]
		public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

		// ReSharper disable once UnusedAutoPropertyAccessor.Global - used by Json.Net
		public Service Service { get; set; }

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public Api.System System { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="Labels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => !Labels.IsEmpty();
	}
}
