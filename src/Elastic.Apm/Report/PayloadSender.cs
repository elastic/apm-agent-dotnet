using System;
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
        private readonly IConfig agentConfig;
        private readonly AbstractLogger logger;
        private readonly Uri serverUrlBase;

        /// <summary>
        /// The work of sending data back to the server is done on this thread
        /// </summary>
        private Thread workerThread;

        /// <summary>
        /// Contains data that will be sent to the server
        /// </summary>
        private BlockingCollection<Payload> payloads = new BlockingCollection<Payload>();

        public PayloadSender()
        {
            agentConfig = Apm.Agent.Config;
            logger = Apm.Agent.CreateLogger(nameof(PayloadSender));
            serverUrlBase = agentConfig.ServerUrls[0];
            workerThread = new Thread(StartWork)
            {
                IsBackground = true
            };
            workerThread.Start();
        }

        public void QueuePayload(Payload payload)
        {
            payloads.Add(payload);
        }

        public async void StartWork()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = serverUrlBase;

            while (true)
            {
                var item = payloads.Take();

                try
                {
                    string json = JsonConvert.SerializeObject(item, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await httpClient.PostAsync(Consts.IntakeV1Transactions, content);

                    var isSucc = result.IsSuccessStatusCode;
                    var str = await result.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    logger.LogWarning($"Failed sending transaction {item.Transactions.FirstOrDefault()?.Name}");
                    logger.LogDebug($"{e.GetType().Name}: {e.Message}");
                }
            }
        }

        public void Dispose()
        {
            payloads?.Dispose();
            payloads = null;
        }
    }
}
