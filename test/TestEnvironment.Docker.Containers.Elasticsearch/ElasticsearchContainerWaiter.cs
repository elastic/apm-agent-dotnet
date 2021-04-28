using System;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;

namespace TestEnvironment.Docker.Containers.Elasticsearch
{
    public class ElasticsearchContainerWaiter : BaseContainerWaiter<ElasticsearchContainer>
    {
        public ElasticsearchContainerWaiter(ILogger logger = null)
            : base(logger)
        {
        }

        protected override async Task<bool> PerformCheck(ElasticsearchContainer container, CancellationToken cancellationToken)
        {
            var elastic = new ElasticClient(new Uri(container.GetUrl()));
            var health = await elastic.Cluster.HealthAsync(
                selector: ch => ch
                    .WaitForStatus(WaitForStatus.Yellow)
                    .Level(Level.Cluster)
                    .ErrorTrace(true),
                ct: cancellationToken);

            Logger?.LogDebug(health.DebugInformation);

            return health.IsValid;
        }
    }
}
