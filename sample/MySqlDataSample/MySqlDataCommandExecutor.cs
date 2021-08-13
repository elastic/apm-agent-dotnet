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
using MySql.Data.MySqlClient;

namespace MySqlDataSample
{
	public class MySqlCommandExecutor : DbCommandExecutor<MySqlCommand>
	{
		public override string CommandTypeName => nameof(MySqlCommand);

		public override bool SupportsAsyncMethods => true;

		public override void ExecuteNonQuery(MySqlCommand command) => command.ExecuteNonQuery();

		public override Task ExecuteNonQueryAsync(MySqlCommand command) => command.ExecuteNonQueryAsync();

		public override Task ExecuteNonQueryAsync(MySqlCommand command, CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);

		public override void ExecuteScalar(MySqlCommand command) => command.ExecuteScalar();

		public override Task ExecuteScalarAsync(MySqlCommand command) => command.ExecuteScalarAsync();

		public override Task ExecuteScalarAsync(MySqlCommand command, CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);

		public override void ExecuteReader(MySqlCommand command)
		{
			using DbDataReader reader = command.ExecuteReader();
		}

		public override void ExecuteReader(MySqlCommand command, CommandBehavior behavior)
		{
			using DbDataReader reader = command.ExecuteReader(behavior);
		}

		public override async Task ExecuteReaderAsync(MySqlCommand command)
		{
			using var reader = await command.ExecuteReaderAsync();
		}

		public override async Task ExecuteReaderAsync(MySqlCommand command, CommandBehavior behavior)
		{
			using var reader = await command.ExecuteReaderAsync(behavior);
		}

		public override async Task ExecuteReaderAsync(MySqlCommand command, CancellationToken cancellationToken)
		{
			using var reader = await command.ExecuteReaderAsync(cancellationToken);
		}

		public override async Task ExecuteReaderAsync(MySqlCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
		{
			using var reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
		}
	}
}
