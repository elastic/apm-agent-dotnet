// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet.NetStandard;

namespace SqlClientSample
{
	public class SqlCommandExecutor : DbCommandExecutor<SqlCommand>
	{
        public override string CommandTypeName => nameof(SqlCommand);

        public override bool SupportsAsyncMethods => true;

        public override void ExecuteNonQuery(SqlCommand command) => command.ExecuteNonQuery();

        public override Task ExecuteNonQueryAsync(SqlCommand command) => command.ExecuteNonQueryAsync();

        public override Task ExecuteNonQueryAsync(SqlCommand command, CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);

        public override void ExecuteScalar(SqlCommand command) => command.ExecuteScalar();

        public override Task ExecuteScalarAsync(SqlCommand command) => command.ExecuteScalarAsync();

        public override Task ExecuteScalarAsync(SqlCommand command, CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);

        public override void ExecuteReader(SqlCommand command)
        {
            using DbDataReader reader = command.ExecuteReader();
        }

        public override void ExecuteReader(SqlCommand command, CommandBehavior behavior)
        {
            using DbDataReader reader = command.ExecuteReader(behavior);
        }

        public override async Task ExecuteReaderAsync(SqlCommand command)
        {
            using DbDataReader reader = await command.ExecuteReaderAsync();
        }

        public override async Task ExecuteReaderAsync(SqlCommand command, CommandBehavior behavior)
        {
            using DbDataReader reader = await command.ExecuteReaderAsync(behavior);
        }

        public override async Task ExecuteReaderAsync(SqlCommand command, CancellationToken cancellationToken)
        {
            using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        }

        public override async Task ExecuteReaderAsync(SqlCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
        {
            using DbDataReader reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
        }
	}
}
