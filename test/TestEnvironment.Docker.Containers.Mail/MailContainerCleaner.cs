using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Mail
{
    public class MailContainerCleaner : IContainerCleaner<MailContainer>
    {
        private readonly ushort _apiPort;
        private readonly string _deleteEndpoint;
        private readonly ILogger _logger;

        public MailContainerCleaner(ushort apiPort = 8025, string deleteEndpoint = "api/v1/messages", ILogger logger = null)
        {
            _apiPort = apiPort;
            _deleteEndpoint = deleteEndpoint;
            _logger = logger;
        }

        public async Task Cleanup(MailContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var uri = new Uri($"http://" +
                $"{(container.IsDockerInDocker ? container.IPAddress : IPAddress.Loopback.ToString())}:" +
                $"{(container.IsDockerInDocker ? _apiPort : container.Ports[_apiPort])}");

            using (var httpClient = new HttpClient { BaseAddress = uri })
            {
                try
                {
                    var response = await httpClient.DeleteAsync(_deleteEndpoint);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogWarning($"Cleanup issue: server replied with {response.StatusCode} code.");
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogWarning($"Cleanup issue: {e.Message}");
                }
            }
        }

        public Task Cleanup(Container container, CancellationToken token = default) => Cleanup((MailContainer)container, token);
    }
}
