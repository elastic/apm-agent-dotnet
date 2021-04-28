using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Mssql
{
    public class MssqlContainerCleaner : IContainerCleaner<MssqlContainer>
    {
        private const string CleanupCommand = "EXEC sp_MSforeachdb " +
            @"'IF DB_ID(''?'') > 4 BEGIN
                ALTER DATABASE [?] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                DROP DATABASE [?]
            END'";

        private readonly ILogger _logger;

        public MssqlContainerCleaner(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task Cleanup(MssqlContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            using (var connection = new SqlConnection(container.GetConnectionString()))
            using (var command = new SqlCommand(CleanupCommand, connection))
            {
                try
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
                catch (SqlException e)
                {
                    _logger?.LogInformation($"Cleanup issue: {e.Message}");
                }
            }
        }

        public Task Cleanup(Container container, CancellationToken token = default) => Cleanup((MssqlContainer)container, token);
    }
}
