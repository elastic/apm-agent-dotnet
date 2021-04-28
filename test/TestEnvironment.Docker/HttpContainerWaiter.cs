using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker
{
    public class HttpContainerWaiter : BaseContainerWaiter<Container>
    {
        private readonly string _path;
        private readonly bool _isHttps;
        private readonly ushort _httpPort;
        private readonly HttpStatusCode[] _successfulCodes;

        public HttpContainerWaiter(string path, bool isHttps = false, ushort httpPort = 80, ILogger logger = null, HttpStatusCode successfulCode = HttpStatusCode.OK)
            : this(path, isHttps, httpPort, logger, new[] { successfulCode })
        {
        }

        public HttpContainerWaiter(string path, bool isHttps = false, ushort httpPort = 80, ILogger logger = null, params HttpStatusCode[] successfulCodes)
            : base(logger)
        {
            _path = path;
            _isHttps = isHttps;
            _httpPort = httpPort;
            _successfulCodes = successfulCodes;
        }

        protected override async Task<bool> PerformCheck(Container container, CancellationToken cancellationToken)
        {
            var uri = new Uri($"{(_isHttps ? "https" : "http")}://" +
                              $"{(container.IsDockerInDocker ? container.IPAddress : IPAddress.Loopback.ToString())}:" +
                              $"{(container.IsDockerInDocker ? _httpPort : container.Ports[_httpPort])}");

            using var client = new HttpClient { BaseAddress = uri };
            var response = await client.GetAsync(_path, cancellationToken);

            return _successfulCodes.Contains(response.StatusCode);
        }
    }
}
