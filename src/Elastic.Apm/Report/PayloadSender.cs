using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
	/// Responsible for sending the data to the server.
	/// Each instance creates its own thread to do the work. Therefore instances should be reused if possible.
	/// </summary>
	internal class PayloadSender : IDisposable, IPayloadSender
	{
		private static readonly int DnsTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

		private readonly HttpClient _httpClient;
		private readonly AbstractLogger _logger;

		private readonly BatchBlock<object> _queue =
			new BatchBlock<object>(1, new GroupingDataflowBlockOptions
				{ BoundedCapacity = 1_000_000 });

		private readonly JsonSerializerSettings _settings;

		static PayloadSender() => ServicePointManager.DnsRefreshTimeout = DnsTimeout;

		internal PayloadSender(AbstractLogger logger, IConfigurationReader configurationReader, HttpMessageHandler handler = null)
		{
			_logger = logger;
			_settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

			var serverUrlBase = configurationReader.ServerUrls.First();
			var servicePoint = ServicePointManager.FindServicePoint(serverUrlBase);

			servicePoint.ConnectionLeaseTimeout = DnsTimeout;
			servicePoint.ConnectionLimit = 20;

			_httpClient = new HttpClient(handler ?? new HttpClientHandler())
			{
				BaseAddress = serverUrlBase,
			};

			if (configurationReader.SecretToken != null)
			{
				_httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", configurationReader.SecretToken);
			}

			var workerThread = new Thread(StartWork)
			{
				IsBackground = true
			};
			workerThread.Start();
		}

		public void QueuePayload(IPayload payload) => _queue.SendAsync(payload);

		public void QueueTransaction(ITransaction transaction) => throw new NotImplementedException();

		public void QueueSpan(ISpan span) => throw new NotImplementedException();

		public void QueueError(IError error) => _queue.SendAsync(error);

		private async void StartWork()
		{
			while (await _queue.OutputAvailableAsync())
			{
				var batch = await _queue.ReceiveAsync();

				var item = batch.FirstOrDefault();
				try
				{
					var json = JsonConvert.SerializeObject(item, _settings);
					var content = new StringContent(json, Encoding.UTF8, "application/json");

					HttpResponseMessage result = null;
					switch (item)
					{
						case Payload _:
							result = await _httpClient.PostAsync(Consts.IntakeV1Transactions, content);
							break;
						case Error _:
							result = await _httpClient.PostAsync(Consts.IntakeV1Errors, content);
							break;
					}

					// TODO: handle unsuccesful status codes
				}
				catch (Exception e)
				{
					switch (item)
					{
						case Payload p:
							_logger.LogWarning($"Failed sending transaction {p.Transactions.FirstOrDefault()?.Name}");
							_logger.LogDebug($"{e.GetType().Name}: {e.Message}");
							break;
						case Error err:
							_logger.LogWarning($"Failed sending Error {err.Errors[0]?.Id}");
							_logger.LogDebug($"{e.GetType().Name}: {e.Message}");
							break;
					}
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}

		public void Dispose()
		{
			_httpClient.Dispose();
			_queue.Complete();
		}
	}
}
