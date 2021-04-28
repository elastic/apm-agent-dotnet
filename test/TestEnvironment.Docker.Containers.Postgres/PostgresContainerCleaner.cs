using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TestEnvironment.Docker.Containers.Postgres
{
    public class PostgresContainerCleaner : IContainerCleaner<PostgresContainer>
    {
        private readonly ILogger _logger;
        private readonly string _userName;

        public PostgresContainerCleaner(ILogger logger, string userName)
        {
            _logger = logger;
            _userName = userName;
        }

        public async Task Cleanup(PostgresContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var cleanUpQuery = $"DROP OWNED BY {_userName}";

            using (var connection = new NpgsqlConnection(container.GetConnectionString()))
            using (var cleanUpCommand = new NpgsqlCommand(cleanUpQuery, connection))
            {
                try
                {
                    await connection.OpenAsync();
                    await cleanUpCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Postgres cleanup issue: {ex.Message}");
                }
            }
        }

        public Task Cleanup(Container container, CancellationToken token = default) => Cleanup((PostgresContainer)container, token);
    }
}
