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
using Elastic.Apm.Logging;
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
		private static readonly int DnsTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

		private readonly BatchBlock<object> _eventQueue =
			new BatchBlock<object>(20);

		private readonly HttpClient _httpClient;
		private readonly IApmLogger _logger;

		internal readonly Api.System System;

		private CancellationTokenSource _batchBlockReceiveAsyncCts;

		private readonly PayloadItemSerializer _payloadItemSerializer = new PayloadItemSerializer();

		private readonly SingleThreadTaskScheduler _singleThreadTaskScheduler = new SingleThreadTaskScheduler(CancellationToken.None);
		private readonly Metadata _metadata;

		public PayloadSenderV2(IApmLogger logger, IConfigurationReader configurationReader, Service service, Api.System system,
			HttpMessageHandler handler = null
		)
		{
			var service1 = service;
			System = system;
			_metadata = new Metadata { Service = service1, System = System };
			_logger = logger?.Scoped(nameof(PayloadSenderV2));

			var serverUrlBase = configurationReader.ServerUrls.First();
			var servicePoint = ServicePointManager.FindServicePoint(serverUrlBase);

			try
			{
				servicePoint.ConnectionLeaseTimeout = DnsTimeout;
			}
			catch (Exception e)
			{
				_logger.Error()
					?.LogException(e,
						"Failed setting servicePoint.ConnectionLeaseTimeout - default ConnectionLeaseTimeout from HttpClient will be used");
			}

			servicePoint.ConnectionLimit = 20;

			_httpClient = new HttpClient(handler ?? new HttpClientHandler()) { BaseAddress = serverUrlBase };
			_httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue($"elasticapm-{Consts.AgentName}", AdaptUserAgentValue(service1.Agent.Version)));
			_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("System.Net.Http",
				AdaptUserAgentValue(typeof(HttpClient).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version)));
			_httpClient.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue(AdaptUserAgentValue(service1.Runtime.Name), AdaptUserAgentValue(service1.Runtime.Version)));

			if (configurationReader.SecretToken != null)
			{
				_httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", configurationReader.SecretToken);
			}
			Task.Factory.StartNew(
				() =>
				{
					try
					{
#pragma warning disable 4014
						DoWork();
#pragma warning restore 4014
					}
					catch (TaskCanceledException ex)
					{
						_logger?.Debug()?.LogExceptionWithCaller(ex);
					}
				}, CancellationToken.None, TaskCreationOptions.LongRunning, _singleThreadTaskScheduler);

			// Replace invalid characters by underscore. All invalid characters can be found at
			// https://github.com/dotnet/corefx/blob/e64cac6dcacf996f98f0b3f75fb7ad0c12f588f7/src/System.Net.Http/src/System/Net/Http/HttpRuleParser.cs#L41
			string AdaptUserAgentValue(string value) => Regex.Replace(value, "[ /()<>@,:;={}?\\[\\]\"\\\\]", "_");
		}

		public void QueueTransaction(ITransaction transaction)
		{
			var res = _eventQueue.Post(transaction);
			_logger.Debug()
				?.Log(!res
					? "Failed adding Transaction to the queue, {Transaction}"
					: "Transaction added to the queue, {Transaction}", transaction);

			_eventQueue.TriggerBatch();
		}

		public void QueueSpan(ISpan span)
		{
			var res = _eventQueue.Post(span);
			_logger.Debug()
				?.Log(!res
					? "Failed adding Span to the queue, {Span}"
					: "Span added to the queue, {Span}", span);
		}

		public void QueueMetrics(IMetricSet metricSet)
		{
			var res = _eventQueue.Post(metricSet);
			_logger.Debug()
				?.Log(!res
					? "Failed adding MetricSet to the queue, {MetricSet}"
					: "MetricSet added to the queue, {MetricSet}", metricSet);
		}

		public void QueueError(IError error)
		{
			var res = _eventQueue.Post(error);
			_logger.Debug()
				?.Log(!res
					? "Failed adding Error to the queue, {Error}"
					: "Error added to the queue, {Error}", error);
		}

		private async Task DoWork()
		{
			_batchBlockReceiveAsyncCts = new CancellationTokenSource();
			while (true)
			{
				var queueItems = await _eventQueue.ReceiveAsync(_batchBlockReceiveAsyncCts.Token);
				await ProcessQueueItems(queueItems);
			}
			// ReSharper disable once FunctionNeverReturns
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
						case Metrics.MetricSet _:
							ndjson.AppendLine("{\"metricset\": " + serialized + "}");
							break;
					}
					_logger?.Trace()?.Log("Serialized item to send: {ItemToSend} as {SerializedItemToSend}", item, serialized);
				}

				var content = new StringContent(ndjson.ToString(), Encoding.UTF8, "application/x-ndjson");

				var result = await _httpClient.PostAsync(Consts.IntakeV2Events, content);

				if (result != null && !result.IsSuccessStatusCode)
					_logger?.Error()?.Log("Failed sending event. {ApmServerResponse}", await result.Content.ReadAsStringAsync());
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
						e, "Failed sending events. Following events were not transferred successfully to the server ({ApmServerUrl}):\n{items}",
						_httpClient.BaseAddress,
						string.Join($",{Environment.NewLine}", queueItems.ToArray()));
			}
		}

		public void Dispose() => _batchBlockReceiveAsyncCts?.Dispose();
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
		[ThreadStatic]
		private static bool _isExecuting;

		private readonly CancellationToken _cancellationToken;

		private readonly BlockingCollection<Task> _taskQueue;

		public SingleThreadTaskScheduler(CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
			_taskQueue = new BlockingCollection<Task>();
			new Thread(RunOnCurrentThread) { Name = "ElasticApmPayloadSender", IsBackground = true }.Start();
		}

		private void RunOnCurrentThread()
		{
			_isExecuting = true;

			try
			{
				foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationToken)) TryExecuteTask(task);
			}
			finally
			{
				_isExecuting = false;
			}
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
