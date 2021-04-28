using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Mssql
{
    public class MssqlContainerWaiter : BaseContainerWaiter<MssqlContainer>
    {
        public MssqlContainerWaiter(ILogger logger = null)
            : base(logger)
        {
        }

        protected override async Task<bool> PerformCheck(MssqlContainer container, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(container.GetConnectionString());
			Logger?.LogInformation($"{container.GetConnectionString()}: checking container state...");
            using var command = new SqlCommand("SELECT @@VERSION", connection);

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return true;
        }

        protected override bool IsRetryable(Exception exception) =>
            exception is InvalidOperationException || exception is NotSupportedException || exception is SqlException;
    }
}
