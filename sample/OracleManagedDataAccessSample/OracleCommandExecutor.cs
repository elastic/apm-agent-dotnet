// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet.NetStandard;
using Oracle.ManagedDataAccess.Client;

namespace OracleManagedDataAccessSample
{
	public class OracleCommandExecutor : DbCommandExecutor<OracleCommand>
	{
		public override string CommandTypeName => nameof(OracleCommand);

		public override bool SupportsAsyncMethods => true;

		public override void ExecuteNonQuery(OracleCommand command) => command.ExecuteNonQuery();

		public override Task ExecuteNonQueryAsync(OracleCommand command) => command.ExecuteNonQueryAsync();

		public override Task ExecuteNonQueryAsync(OracleCommand command, CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);

		public override void ExecuteScalar(OracleCommand command) => command.ExecuteScalar();

		public override Task ExecuteScalarAsync(OracleCommand command) => command.ExecuteScalarAsync();

		public override Task ExecuteScalarAsync(OracleCommand command, CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);

		public override void ExecuteReader(OracleCommand command)
		{
			using DbDataReader reader = command.ExecuteReader();
		}

		public override void ExecuteReader(OracleCommand command, CommandBehavior behavior)
		{
			using DbDataReader reader = command.ExecuteReader(behavior);
		}

		public override async Task ExecuteReaderAsync(OracleCommand command)
		{
			using var reader = await command.ExecuteReaderAsync();
		}

		public override async Task ExecuteReaderAsync(OracleCommand command, CommandBehavior behavior)
		{
			using var reader = await command.ExecuteReaderAsync(behavior);
		}

		public override async Task ExecuteReaderAsync(OracleCommand command, CancellationToken cancellationToken)
		{
			using var reader = await command.ExecuteReaderAsync(cancellationToken);
		}

		public override async Task ExecuteReaderAsync(OracleCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
		{
			using var reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
		}
	}
}
