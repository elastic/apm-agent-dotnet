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
		private readonly AbstractAgentConfig agentConfig;
		private readonly AbstractLogger logger;
		private readonly Uri serverUrlBase;

		/// <summary>
		/// Contains data that will be sent to the server
		/// </summary>
		private BlockingCollection<object> payloads = new BlockingCollection<object>();

		/// <summary>
		/// The work of sending data back to the server is done on this thread
		/// </summary>
		private readonly Thread workerThread;

		public PayloadSender()
		{
			agentConfig = Agent.Config;
			logger = Agent.CreateLogger(nameof(PayloadSender));
			serverUrlBase = agentConfig.ServerUrls[0];
			workerThread = new Thread(StartWork)
			{
				IsBackground = true
			};
			workerThread.Start();
		}

		public void Dispose()
		{
			payloads?.Dispose();
			payloads = null;
		}

		public void QueueError(Error error)
			=> payloads.Add(error);

		public void QueuePayload(Payload payload)
			=> payloads.Add(payload);

		public async void StartWork()
		{
			var httpClient = new HttpClient
			{
				BaseAddress = serverUrlBase
			};

			while (true)
			{
				var item = payloads.Take();

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
					if (item is Payload p)
					{
						logger.LogWarning($"Failed sending transaction {p.Transactions.FirstOrDefault()?.Name}");
						logger.LogDebug($"{e.GetType().Name}: {e.Message}");
					}
					if (item is Error err)
					{
						logger.LogWarning($"Failed sending Error {err.Errors[0]?.Id}");
						logger.LogDebug($"{e.GetType().Name}: {e.Message}");
					}
				}
			}
		}
	}
}
