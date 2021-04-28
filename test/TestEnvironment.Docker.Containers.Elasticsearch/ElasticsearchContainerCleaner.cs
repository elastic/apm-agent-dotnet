using System;
using System.Threading;
using System.Threading.Tasks;
using Nest;

namespace TestEnvironment.Docker.Containers.Elasticsearch
{
    public class ElasticsearchContainerCleaner : IContainerCleaner<ElasticsearchContainer>
    {
        public async Task Cleanup(ElasticsearchContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var elastic = new ElasticClient(new Uri(container.GetUrl()));

            await elastic.Indices.DeleteTemplateAsync("*", ct: token);
            await elastic.Indices.DeleteAsync("*", ct: token);
        }

        public Task Cleanup(Container container, CancellationToken token = default) => Cleanup((ElasticsearchContainer)container, token);
    }
}
