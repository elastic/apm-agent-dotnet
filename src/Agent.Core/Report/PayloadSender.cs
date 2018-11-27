using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Agent.Core.Model.Payload;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

//TODO: It'd be nice to move this into the .csproj
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Concurrent;
using Elastic.Agent.Core.Logging;
using System.Linq;

[assembly: InternalsVisibleTo("Agent.AspNetCore")]
[assembly: InternalsVisibleTo("Agent.EntityFrameworkCore")]
[assembly: InternalsVisibleTo("Agent.Core.Tests")]

namespace Elastic.Agent.Core.Report
{
    /// <summary>
    /// Responsible for sending the data to the server. 
    /// Each instance creates its own thread to do the work. Therefore instances should be reused if possible.
    /// </summary>
    internal class PayloadSender : IDisposable, IPayloadSender
    {
        private readonly Config agentConfig;
        private readonly AbstractLogger logger;

        /// <summary>
        /// The work of sending data back to the server is done on this thread
        /// </summary>
        private Thread workerThread;

        /// <summary>
        /// Contains data that will be sent to the server
        /// </summary>
        private BlockingCollection<Payload> payloads = new BlockingCollection<Payload>();

        public String ServerUrlBase { get; set; } = "http://127.0.0.1:8200";

        public PayloadSender(Config agentConfig)
        {
            this.agentConfig = agentConfig;
            logger = Apm.Agent.CreateLogger(nameof(PayloadSender));
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

            while (true)
            {
                var item = payloads.Take();

                try
                {
                    string json = JsonConvert.SerializeObject(item, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await httpClient.PostAsync(ServerUrlBase + Consts.IntakeV1Transactions, content);

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
