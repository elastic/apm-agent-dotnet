using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TestEnvironment.Docker;

namespace Elastic.Apm.Mongo.IntegrationTests.Fixture.TestEnvironment.Docker
{
	public class MongoContainerCleaner : IContainerCleaner<MongoContainer>
	{
		private readonly ILogger _logger;

		public MongoContainerCleaner(ILogger logger = null) => _logger = logger;

		public async Task Cleanup(MongoContainer container, CancellationToken token = new CancellationToken())
		{
			if (container == null)
			{
				throw new ArgumentNullException(nameof(container));
			}

			var client = new MongoClient(container.GetConnectionString());
			var databaseNames = (await client.ListDatabasesAsync(token)).ToList().Select(x => x["name"].AsString)
				.ToList();
			try
			{
				foreach (var databaseName in databaseNames)
				{
					if (databaseName != "admin" && databaseName != "local")
					{
						await client.DropDatabaseAsync(databaseName, token);
					}
				}
			}
			catch (Exception e)
			{
				_logger?.LogInformation($"MongoDB cleanup issue: {e.Message}");
			}
		}

		public Task Cleanup(Container container, CancellationToken token = new CancellationToken()) =>
			Cleanup((MongoContainer)container, token);
	}
}
