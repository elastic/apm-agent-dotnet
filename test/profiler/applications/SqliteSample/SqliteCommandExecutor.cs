// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet.NetStandard;
using Microsoft.Data.Sqlite;

namespace SqliteSample
{
	public class SqliteCommandExecutor : DbCommandExecutor<SqliteCommand>
	{
		public override string CommandTypeName => nameof(SqliteCommand);

		public override bool SupportsAsyncMethods => true;

		public override void ExecuteNonQuery(SqliteCommand command) => command.ExecuteNonQuery();

		public override Task ExecuteNonQueryAsync(SqliteCommand command) => command.ExecuteNonQueryAsync();

		public override Task ExecuteNonQueryAsync(SqliteCommand command, CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);

		public override void ExecuteScalar(SqliteCommand command) => command.ExecuteScalar();

		public override Task ExecuteScalarAsync(SqliteCommand command) => command.ExecuteScalarAsync();

		public override Task ExecuteScalarAsync(SqliteCommand command, CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);

		public override void ExecuteReader(SqliteCommand command)
		{
			using DbDataReader reader = command.ExecuteReader();
		}

		public override void ExecuteReader(SqliteCommand command, CommandBehavior behavior)
		{
			using DbDataReader reader = command.ExecuteReader(behavior);
		}

		public override async Task ExecuteReaderAsync(SqliteCommand command)
		{
			using DbDataReader reader = await command.ExecuteReaderAsync();
		}

		public override async Task ExecuteReaderAsync(SqliteCommand command, CommandBehavior behavior)
		{
			using DbDataReader reader = await command.ExecuteReaderAsync(behavior);
		}

		public override async Task ExecuteReaderAsync(SqliteCommand command, CancellationToken cancellationToken)
		{
			using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
		}

		public override async Task ExecuteReaderAsync(SqliteCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
		{
			using DbDataReader reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
		}
	}
}
