using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TestEnvironment.Docker.Containers.Mongo
{
    public class MongoSingleReplicaSetContainerWaiter : BaseContainerWaiter<MongoSingleReplicaSetContainer>
    {
        public MongoSingleReplicaSetContainerWaiter(ILogger logger)
            : base(logger)
        {
        }

        protected override async Task<bool> PerformCheck(
            MongoSingleReplicaSetContainer container,
            CancellationToken cancellationToken)
        {
            await new MongoClient(container.GetDirectNodeConnectionString()).GetDatabase("admin")
                .RunCommandAsync(
                    new BsonDocumentCommand<BsonDocument>(new BsonDocument("ping", 1)),
                    cancellationToken: cancellationToken);
            return true;
        }

        protected override bool IsRetryable(Exception exception) =>
            base.IsRetryable(exception) && !(exception is MongoAuthenticationException);
    }
}
