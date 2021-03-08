using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TestEnvironment.Docker;

namespace Elastic.Apm.Mongo.IntegrationTests.Fixture.TestEnvironment.Docker
{
	public class MongoContainerWaiter : IContainerWaiter<MongoContainer>
	{
		private readonly ILogger _logger;

		public MongoContainerWaiter(ILogger? logger = null) => _logger = logger;

		public async Task<bool> Wait(MongoContainer? container, CancellationToken cancellationToken)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			try
			{
				_logger?.LogInformation($"MongoDB: checking container state...");

				await new MongoClient(container.GetConnectionString()).ListDatabasesAsync(cancellationToken);

				_logger?.LogInformation($"MongoDB: container is Up!");

				return true;
			}
			catch (Exception e)
			{
				_logger?.LogError($"MongoDB: check failed with exception {e.Message}");
				return false;
			}
		}

		public Task<bool> Wait(Container container, CancellationToken cancellationToken) =>
			Wait(container as MongoContainer, cancellationToken);
	}
}
