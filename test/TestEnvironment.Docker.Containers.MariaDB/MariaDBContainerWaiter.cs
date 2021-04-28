using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace TestEnvironment.Docker.Containers.MariaDB
{
    public class MariaDBContainerWaiter : BaseContainerWaiter<MariaDBContainer>
    {
        public MariaDBContainerWaiter(ILogger logger = null)
            : base(logger)
        {
        }

        protected override async Task<bool> PerformCheck(MariaDBContainer container, CancellationToken cancellationToken)
        {
            // don't use await using here due to
            // System.MissingMethodException : Method not found: 'System.Threading.Tasks.Task MySql.Data.MySqlClient.MySqlConnection.DisposeAsync()'.
            using var connection = new MySqlConnection(container.GetConnectionString());
            using var command = new MySqlCommand("select @@version", connection);

            await command.Connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return true;
        }

        protected override bool IsRetryable(Exception exception) =>
            exception is InvalidOperationException || exception is NotSupportedException || exception is MySqlException;
    }
}
