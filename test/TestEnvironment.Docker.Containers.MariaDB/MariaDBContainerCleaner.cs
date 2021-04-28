using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace TestEnvironment.Docker.Containers.MariaDB
{
    public class MariaDBContainerCleaner : IContainerCleaner<MariaDBContainer>
    {
        private const string GetAllDatabasesCommand = "show databases;";
        private const string DropDatabaseCommand = "drop database {0}";

        private static readonly string[] SystemDatabases = { "information_schema", "mysql", "performance_schema" };

        private readonly ILogger _logger;

        public MariaDBContainerCleaner(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task Cleanup(MariaDBContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            using (var connection = new MySqlConnection(container.GetConnectionString()))
            using (var getDatabasesCommand = new MySqlCommand(GetAllDatabasesCommand, connection))
            {
                await getDatabasesCommand.Connection.OpenAsync();

                try
                {
                    var reader = await getDatabasesCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var databaseName = reader.GetString(0);

                        if (SystemDatabases.All(dn => !dn.Equals(databaseName, StringComparison.OrdinalIgnoreCase)))
                        {
                            using (var dropConnection = new MySqlConnection(container.GetConnectionString()))
                            using (var dropCommand = new MySqlCommand(string.Format(DropDatabaseCommand, databaseName), dropConnection))
                            {
                                await dropConnection.OpenAsync();
                                await dropCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                catch (MySqlException e)
                {
                    _logger?.LogWarning($"Cleanup issue: {e.Message}");
                }
            }
        }

        public Task Cleanup(Container container, CancellationToken token = default) => Cleanup((MariaDBContainer)container, token);
    }
}
