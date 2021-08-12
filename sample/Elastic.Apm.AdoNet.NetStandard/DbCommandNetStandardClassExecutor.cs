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

namespace Elastic.Apm.AdoNet.NetStandard
{
	public class DbCommandNetStandardClassExecutor : DbCommandExecutor<DbCommand>
	{
		public override string CommandTypeName => nameof(DbCommand) + "-NetStandard";

		public override bool SupportsAsyncMethods => true;

		public override void ExecuteNonQuery(DbCommand command) => command.ExecuteNonQuery();

		public override Task ExecuteNonQueryAsync(DbCommand command) => command.ExecuteNonQueryAsync();

		public override Task ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);

		public override void ExecuteScalar(DbCommand command) => command.ExecuteScalar();

		public override Task ExecuteScalarAsync(DbCommand command) => command.ExecuteScalarAsync();

		public override Task ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);

		public override void ExecuteReader(DbCommand command)
		{
			using var reader = command.ExecuteReader();
		}

		public override void ExecuteReader(DbCommand command, CommandBehavior behavior)
		{
			using var reader = command.ExecuteReader(behavior);
		}

		public override async Task ExecuteReaderAsync(DbCommand command)
		{
			using var reader = await command.ExecuteReaderAsync();
		}

		public override async Task ExecuteReaderAsync(DbCommand command, CommandBehavior behavior)
		{
			using var reader = await command.ExecuteReaderAsync(behavior);
		}

		public override async Task ExecuteReaderAsync(DbCommand command, CancellationToken cancellationToken)
		{
			using var reader = await command.ExecuteReaderAsync(cancellationToken);
		}

		public override async Task ExecuteReaderAsync(DbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
		{
			using var reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
		}
	}
}
