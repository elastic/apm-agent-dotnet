using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace TestEnvironment.Docker.Containers.Mongo
{
    public class MongoContainerCleaner : IContainerCleaner<MongoContainer>,
        IContainerCleaner<MongoSingleReplicaSetContainer>
    {
        private readonly ILogger _logger;

        public MongoContainerCleaner(ILogger logger = null)
        {
            _logger = logger;
        }

        public Task Cleanup(MongoContainer container, CancellationToken token = default) =>
            Cleanup((IMongoContainer)container, token);

        public Task Cleanup(MongoSingleReplicaSetContainer container, CancellationToken token = default) =>
            Cleanup((IMongoContainer)container, token);

        public Task Cleanup(Container container, CancellationToken token = default) =>
            Cleanup((IMongoContainer)container, token);

        private async Task Cleanup(IMongoContainer container, CancellationToken cancellationToken)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var client = new MongoClient(container.GetConnectionString());
            var databaseNames = (await client.ListDatabasesAsync(cancellationToken))
                .ToList()
                .Select(x => x["name"].AsString);

            try
            {
                foreach (var databaseName in databaseNames)
                {
                    if (databaseName != "admin" && databaseName != "local")
                    {
                        await client.DropDatabaseAsync(databaseName, cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogInformation($"MongoDB cleanup issue: {e.Message}");
            }
        }
    }
}
