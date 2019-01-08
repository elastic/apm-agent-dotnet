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
        private readonly AbstractAgentConfig _agentConfig;
        private readonly AbstractLogger _logger;
        private readonly Uri _serverUrlBase;

        /// <summary>
        /// The work of sending data back to the server is done on this thread
        /// </summary>
        private Thread _workerThread;

        /// <summary>
        /// Contains data that will be sent to the server
        /// </summary>
        private BlockingCollection<Object> _payloads = new BlockingCollection<Object>();

        public PayloadSender()
        {
            _agentConfig = Apm.Agent.Config;
            _logger = Apm.Agent.CreateLogger(nameof(PayloadSender));
            _serverUrlBase = _agentConfig.ServerUrls[0];
            _workerThread = new Thread(StartWork)
            {
                IsBackground = true
            };
            _workerThread.Start();
        }

        public void QueuePayload(Payload payload)
         => _payloads.Add(payload);

        public void QueueError(Error error)
         => _payloads.Add(error);

        public async void StartWork()
        {
            HttpClient httpClient = new HttpClient
            {
                BaseAddress = _serverUrlBase
            };

            while (true)
            {
                var item = _payloads.Take();

                try
                {
                    string json = JsonConvert.SerializeObject(item, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
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
                        _logger.LogWarning($"Failed sending transaction {p.Transactions.FirstOrDefault()?.Name}");
                        _logger.LogDebug($"{e.GetType().Name}: {e.Message}");
                    }
                    if(item is Error err)
                    {
                        _logger.LogWarning($"Failed sending Error {err.Errors[0]?.Id}");
                        _logger.LogDebug($"{e.GetType().Name}: {e.Message}");
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
