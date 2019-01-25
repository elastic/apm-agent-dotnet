using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
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
		private readonly AbstractLogger _logger;
		private readonly Uri _serverUrlBase;

		internal PayloadSender(AbstractLogger logger, IConfigurationReader configurationReader)
		{
			_logger = logger;
			_serverUrlBase = configurationReader.ServerUrls.First();
			var workerThread = new Thread(StartWork)
			{
				IsBackground = true
			};
			workerThread.Start();
		}

		/// <summary>
		/// Contains data that will be sent to the server
		/// </summary>
		private BlockingCollection<object> _payloads = new BlockingCollection<object>();

		public void QueuePayload(IPayload payload) => _payloads.Add(payload);

		public void QueueError(IError error) => _payloads.Add(error);

		private async void StartWork()
		{
			var httpClient = new HttpClient
			{
				BaseAddress = _serverUrlBase
			};

			while (true)
			{
				var item = _payloads.Take();

				try
				{
					var json = JsonConvert.SerializeObject(item,
						new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
					var content = new StringContent(json, Encoding.UTF8, "application/json");

					switch (item)
					{
						case Payload _:
							await httpClient.PostAsync(Consts.IntakeV1Transactions, content);
							break;
						case Error _:
							await httpClient.PostAsync(Consts.IntakeV1Errors, content);
							break;
					}
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
			_payloads?.Dispose();
			_payloads = null;
		}
	}
}
