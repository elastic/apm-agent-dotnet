using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;

namespace Elastic.Apm.Report
{
	/// <summary>
	/// Responsible for sending the data to the server. Implements Intake V2.
	/// Each instance creates its own thread to do the work. Therefore, instances should be reused if possible.
	/// </summary>
	internal class PayloadSenderV2 : IPayloadSender, IDisposable
	{
		private const string ThisClassName = nameof(PayloadSenderV2);
		private static readonly int DnsTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

		internal readonly Api.System System;

		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly DisposableHelper _disposableHelper = new DisposableHelper();
		private readonly BatchBlock<object> _eventQueue;

		private readonly TimeSpan _flushInterval;

		private readonly HttpClient _httpClient;
		private readonly IApmLogger _logger;
		private readonly int _maxQueueEventCount;
		private readonly Metadata _metadata;

		private readonly PayloadItemSerializer _payloadItemSerializer = new PayloadItemSerializer();
		private readonly SingleThreadTaskScheduler _singleThreadTaskScheduler;

		public PayloadSenderV2(IApmLogger logger, IConfigurationReader configurationReader, Service service, Api.System system,
			HttpMessageHandler handler = null
		)
		{
			_logger = logger?.Scoped(ThisClassName);

			System = system;
			_metadata = new Metadata { Service = service, System = System };

			if (configurationReader.MaxQueueEventCount < configurationReader.MaxBatchEventCount)
			{
				_logger?.Error()
					?.Log(
						"MaxQueueEventCount is less than MaxBatchEventCount - using MaxBatchEventCount as MaxQueueEventCount."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " MaxBatchEventCount: {MaxBatchEventCount}.",
						configurationReader.MaxQueueEventCount, configurationReader.MaxBatchEventCount);

				_maxQueueEventCount = configurationReader.MaxBatchEventCount;
			}
			else
				_maxQueueEventCount = configurationReader.MaxQueueEventCount;

			_flushInterval = configurationReader.FlushInterval;
			_eventQueue = new BatchBlock<object>(configurationReader.MaxBatchEventCount);

			_cancellationTokenSource = new CancellationTokenSource();
			_singleThreadTaskScheduler = new SingleThreadTaskScheduler(logger, _cancellationTokenSource.Token);

			var serverUrlBase = configurationReader.ServerUrls.First();
			var servicePoint = ServicePointManager.FindServicePoint(serverUrlBase);

			try
			{
				servicePoint.ConnectionLeaseTimeout = DnsTimeout;
			}
			catch (Exception e)
			{
				_logger.Warning()
					?.LogException(e,
						"Failed setting servicePoint.ConnectionLeaseTimeout - default ConnectionLeaseTimeout from HttpClient will be used. "
						+ "Unless you notice connection issues between the APM Server and the agent, no action needed.");
			}

			servicePoint.ConnectionLimit = 20;

			_httpClient = new HttpClient(handler ?? new HttpClientHandler()) { BaseAddress = serverUrlBase };
			_httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue($"elasticapm-{Consts.AgentName}", AdaptUserAgentValue(service.Agent.Version)));
			_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("System.Net.Http",
				AdaptUserAgentValue(typeof(HttpClient).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version)));
			_httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue(AdaptUserAgentValue(service.Runtime.Name), AdaptUserAgentValue(service.Runtime.Version)));

			if (configurationReader.SecretToken != null)
			{
				_httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", configurationReader.SecretToken);
			}

#pragma warning disable 4014
			Task.Factory.StartNew(DoWork, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, _singleThreadTaskScheduler);
#pragma warning restore 4014

			_logger.Debug()?.Log("Enqueued " + ThisClassName + "." + nameof(DoWork));

			// Replace invalid characters by underscore. All invalid characters can be found at
			// https://github.com/dotnet/corefx/blob/e64cac6dcacf996f98f0b3f75fb7ad0c12f588f7/src/System.Net.Http/src/System/Net/Http/HttpRuleParser.cs#L41
			string AdaptUserAgentValue(string value)
			{
				return Regex.Replace(value, "[ /()<>@,:;={}?\\[\\]\"\\\\]", "_");
			}
		}

		private long _eventQueueCount;

		internal Thread Thread => _singleThreadTaskScheduler.Thread;

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

		public void Dispose() =>
			_disposableHelper.DoOnce(_logger, ThisClassName, () =>
			{
				_logger.Debug()?.Log("Signalling _cancellationTokenSource");
				_cancellationTokenSource.Cancel();

				_logger.Debug()?.Log("Waiting for _singleThreadTaskScheduler thread `{ThreadName}' to exit", _singleThreadTaskScheduler.Thread.Name);
				_singleThreadTaskScheduler.Thread.Join();

				_logger.Debug()?.Log("_singleThreadTaskScheduler thread exited - disposing of _cancellationTokenSource and exiting");
				_cancellationTokenSource.Dispose();
			});

		private void ThrowIfDisposed()
		{
			if (_disposableHelper.HasStarted) throw new ObjectDisposedException( /* objectName: */ ThisClassName);
		}

		private Task DoWork() =>
			ExceptionUtils.DoSwallowingExceptions(_logger, async () =>
				{
					while (true) await ProcessQueueItems(await ReceiveBatchAsync());
					// ReSharper disable once FunctionNeverReturns
				}
				, dbgCallerMethodName: ThisClassName + "." + DbgUtils.GetCurrentMethodName());

		private async Task<object[]> ReceiveBatchAsync()
		{
			var receiveAsyncTask = _eventQueue.ReceiveAsync(_cancellationTokenSource.Token);

			if (_flushInterval == TimeSpan.Zero)
				_logger.Trace()?.Log("Waiting for data to send... (not using FlushInterval timer because FlushInterval is 0)");
			else
			{
				_logger.Trace()?.Log("Waiting for data to send... FlushInterval: {FlushInterval}", _flushInterval);
				while (true)
				{
					if (await TryAwaitOrTimeout(receiveAsyncTask, _flushInterval, _cancellationTokenSource.Token)) break;

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
		///
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
				var metadataJson = _payloadItemSerializer.SerializeObject(_metadata);
				var ndjson = new StringBuilder();
				ndjson.AppendLine("{\"metadata\": " + metadataJson + "}");

				foreach (var item in queueItems)
				{
					var serialized = _payloadItemSerializer.SerializeObject(item);
					switch (item)
					{
						case Transaction _:
							ndjson.AppendLine("{\"transaction\": " + serialized + "}");
							break;
						case Span _:
							ndjson.AppendLine("{\"span\": " + serialized + "}");
							break;
						case Error _:
							ndjson.AppendLine("{\"error\": " + serialized + "}");
							break;
						case MetricSet _:
							ndjson.AppendLine("{\"metricset\": " + serialized + "}");
							break;
					}
					_logger?.Trace()?.Log("Serialized item to send: {ItemToSend} as {SerializedItemToSend}", item, serialized);
				}

				var content = new StringContent(ndjson.ToString(), Encoding.UTF8, "application/x-ndjson");

				var result = await _httpClient.PostAsync(Consts.IntakeV2Events, content, _cancellationTokenSource.Token);

				if (result != null && !result.IsSuccessStatusCode)
				{
					_logger?.Error()
						?.Log("Failed sending event. " +
							"APM Server response: status code: {ApmServerResponseStatusCode}, content: \n{ApmServerResponseContent}",
							result.StatusCode, await result.Content.ReadAsStringAsync());
				}
				else
				{
					_logger?.Debug()
						?.Log($"Sent items to server: {Environment.NewLine}{{items}}", string.Join($",{Environment.NewLine}", queueItems.ToArray()));
				}
			}
			catch (Exception e)
			{
				_logger?.Warning()
					?.LogException(
						e,
						"Failed sending events. Following events were not transferred successfully to the server ({ApmServerUrl}):\n{SerializedItems}",
						_httpClient.BaseAddress,
						string.Join($",{Environment.NewLine}", queueItems.ToArray()));
			}
		}
	}

	internal class Metadata
	{
		// ReSharper disable once UnusedAutoPropertyAccessor.Global - used by Json.Net
		public Service Service { get; set; }

		public Api.System System { get; set; }
	}

	//Credit: https://stackoverflow.com/a/30726903/1783306
	internal sealed class SingleThreadTaskScheduler : TaskScheduler
	{
		private const string ThisClassName = nameof(PayloadSenderV2) + "." + nameof(SingleThreadTaskScheduler);

		[ThreadStatic]
		private static bool _isExecuting;

		private readonly CancellationToken _cancellationToken;

		private readonly IApmLogger _logger;

		private readonly BlockingCollection<Task> _taskQueue;

		public SingleThreadTaskScheduler(IApmLogger logger, CancellationToken cancellationToken)
		{
			_logger = logger?.Scoped(ThisClassName);
			_cancellationToken = cancellationToken;
			_taskQueue = new BlockingCollection<Task>();
			Thread = new Thread(RunOnCurrentThread) { Name = "ElasticApmPayloadSender", IsBackground = true };
			Thread.Start();
		}

		internal Thread Thread { get; }

		private void RunOnCurrentThread()
		{
			_logger.Debug()?.Log("`{ThreadName}' thread started", Thread.CurrentThread.Name);

			_isExecuting = true;

			ExceptionUtils.DoSwallowingExceptions(_logger, () =>
				{
					foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationToken)) TryExecuteTask(task);
				}
				, dbgCallerMethodName: $"`{Thread.CurrentThread.Name}' (ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}) thread");

			_isExecuting = false;
		}

		protected override IEnumerable<Task> GetScheduledTasks() => null;

		protected override void QueueTask(Task task) => _taskQueue.Add(task, _cancellationToken);

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			// We'd need to remove the task from queue if it was already queued.
			// That would be too hard.
			if (taskWasPreviouslyQueued) return false;

			return _isExecuting && TryExecuteTask(task);
		}
	}
}
