﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
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

		public PayloadSender(AbstractLogger logger, IConfigurationReader configurationReader)
		{
			_logger = logger;
			_serverUrlBase = configurationReader.ServerUrls.First();
			_workerThread = new Thread(StartWork)
			{
				IsBackground = true
			};
			_workerThread.Start();
		}

		/// <summary>
		/// Contains data that will be sent to the server
		/// </summary>
		private BlockingCollection<object> _payloads = new BlockingCollection<object>();

		/// <summary>
		/// The work of sending data back to the server is done on this thread
		/// </summary>
		private readonly Thread _workerThread;

		public void QueuePayload(Payload payload) => _payloads.Add(payload);

		public void QueueError(Error error) => _payloads.Add(error);

		public async void StartWork()
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

					HttpResponseMessage result = null;
					switch (item)
					{
						case Payload p:
							result = await httpClient.PostAsync(Consts.IntakeV1Transactions, content);
							break;
						case Error e:
							result = await httpClient.PostAsync(Consts.IntakeV1Errors, content);
							break;
					}

					var isSucc = result.IsSuccessStatusCode;
					var str = await result.Content.ReadAsStringAsync();
				}
				catch (Exception e)
				{
					switch (item) {
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
		}

		public void Dispose()
		{
			_payloads?.Dispose();
			_payloads = null;
		}
	}
}
