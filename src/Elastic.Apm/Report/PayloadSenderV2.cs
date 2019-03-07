using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report
{
	/// <summary>
	/// Responsible for sending the data to the server. Implements Intake V2.
	/// Each instance creates its own thread to do the work. Therefore instances should be reused if possible.
	/// </summary>
	public class PayloadSenderV2 : IPayloadSender
	{
		private readonly ScopedLogger _logger;
		private readonly Service _service;

		private readonly JsonSerializerSettings _settings;

		private static readonly int DnsTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

		private readonly BatchBlock<object> _eventQueue =
			new BatchBlock<object>(20, new GroupingDataflowBlockOptions
				{ BoundedCapacity = 1_000_000 });

		private readonly HttpClient _httpClient;


		public void QueueTransaction(ITransaction transaction)
		{
			_eventQueue.Post(transaction);
			_eventQueue.TriggerBatch();
		}

		public void QueueSpan(ISpan span) => _eventQueue.Post(span);

		public void QueueError(IError error) => _eventQueue.Post(error);

		public PayloadSenderV2(IApmLogger logger, IConfigurationReader configurationReader, Service service, HttpMessageHandler handler = null)
		{
			_settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None };
			_service = service;
			_logger = logger?.Scoped(nameof(PayloadSenderV2));

			var serverUrlBase = configurationReader.ServerUrls.First();
			var servicePoint = ServicePointManager.FindServicePoint(serverUrlBase);

			servicePoint.ConnectionLeaseTimeout = DnsTimeout;
			servicePoint.ConnectionLimit = 20;

			_httpClient = new HttpClient(handler ?? new HttpClientHandler())
			{
				BaseAddress = serverUrlBase
			};

			if (configurationReader.SecretToken != null)
			{
				_httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", configurationReader.SecretToken);
			}

			Task.Factory.StartNew(
				async () =>
				{
					while (true)
					{
						try
						{
							await DoWork();
						}
						catch (TaskCanceledException ex)
						{
							_logger.LogDebugException(ex);
						}
					}
				}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		private async Task DoWork()
		{
			while (true)
			{
				var queueItems = await _eventQueue.ReceiveAsync();
				try
				{
					var metadata = new Metadata { Service = _service };
					var metadataJson = JsonConvert.SerializeObject(metadata, _settings);
					var json = new StringBuilder();
					json.Append("{\"metadata\": " + metadataJson + "}" + "\n");

					foreach (var item in queueItems)
					{
						var serialized = JsonConvert.SerializeObject(item, _settings);
						switch (item)
						{
							case Transaction _:
								json.AppendLine("{\"transaction\": " + serialized + "}");
								break;
							case Span _:
								json.AppendLine("{\"span\": " + serialized + "}");
								break;
							case Error _:
								json.AppendLine("{\"error\": " + serialized + "}");
								break;
						}
					}

					var content = new StringContent(json.ToString(), Encoding.UTF8, "application/x-ndjson");

					var result = await _httpClient.PostAsync(Consts.IntakeV2Events, content);

					if (result != null && !result.IsSuccessStatusCode)
					{
						var str = await result.Content.ReadAsStringAsync();
						_logger.LogError($"Failed sending event. {str}");
					}
					if (result != null && result.IsSuccessStatusCode)
					{
						var sb = new StringBuilder();
						sb.AppendLine("Sent items to server:");
						foreach (var item in queueItems)
						{
							sb.AppendLine(item.ToString());
						}
						_logger.LogDebug(sb.ToString());
					}
				}
				catch (Exception e)
				{
					var sb = new StringBuilder();
					sb.AppendLine("Following events were not transferred successfully to the server:");
					foreach (var item in queueItems)
					{
						sb.AppendLine(item.ToString());
					}

					_logger.LogWarning(sb.ToString());
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}
	}

	internal class Metadata
	{
		public Service Service { get; set; }
	}
}
