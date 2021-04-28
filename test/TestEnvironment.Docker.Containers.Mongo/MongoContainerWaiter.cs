using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace TestEnvironment.Docker.Containers.Mongo
{
    public class MongoContainerWaiter : BaseContainerWaiter<MongoContainer>
    {
        public MongoContainerWaiter(ILogger logger = null)
            : base(logger)
        {
        }

        protected override async Task<bool> PerformCheck(MongoContainer container, CancellationToken cancellationToken)
        {
            await new MongoClient(container.GetConnectionString()).ListDatabasesAsync(cancellationToken);
            return true;
        }

        protected override bool IsRetryable(Exception exception) =>
            base.IsRetryable(exception) && !(exception is MongoAuthenticationException);
    }
}
