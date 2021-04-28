using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TestEnvironment.Docker.Containers.Mongo
{
    public class MongoSingleReplicaSetContainerInitializer : IContainerInitializer<MongoSingleReplicaSetContainer>
    {
        private readonly string _replicaSetName;

        public MongoSingleReplicaSetContainerInitializer(string replicaSetName)
        {
            if (string.IsNullOrWhiteSpace(replicaSetName))
            {
                throw new ArgumentException("The value must be specified", nameof(replicaSetName));
            }

            _replicaSetName = replicaSetName;
        }

        public async Task<bool> Initialize(
            MongoSingleReplicaSetContainer container,
            CancellationToken cancellationToken)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var mongoClient = new MongoClient(container.GetDirectNodeConnectionString());

            if (await IsInitialized(mongoClient, cancellationToken))
            {
                return true;
            }

            await mongoClient.GetDatabase("admin").RunCommandAsync(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument
                {
                    {
                        "replSetInitiate",
                        new BsonDocument
                        {
                            { "_id", _replicaSetName },
                            {
                                "members",
                                new BsonArray
                                {
                                    new BsonDocument
                                    {
                                        { "_id", 0 },
                                        {
                                            "host",
                                            mongoClient.Settings.Server.ToString()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }), cancellationToken: cancellationToken);

            return true;
        }

        public Task<bool> Initialize(Container container, CancellationToken cancellationToken) =>
            Initialize(container as MongoSingleReplicaSetContainer, cancellationToken);

        private async Task<bool> IsInitialized(IMongoClient mongoClient, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = await mongoClient.GetDatabase("admin")
                    .RunCommandAsync(
                        new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "replSetGetConfig", 1 } }),
                        cancellationToken: cancellationToken);

                return configuration["config"]["_id"].AsString == _replicaSetName &&
                       configuration["config"]["members"].AsBsonArray.Count == 1 &&
                       configuration["config"]["members"].AsBsonArray[0]["_id"] == 0 &&
                       configuration["config"]["members"].AsBsonArray[0]["host"] ==
                       mongoClient.Settings.Server.ToString();
            }
            catch (MongoCommandException exception) when (exception.Code == 94 /*"NotYetInitialized"*/)
            {
                return false;
            }
        }
    }
}
