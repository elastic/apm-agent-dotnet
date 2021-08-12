// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Apm.AdoNet.NetStandard
{
	public class DbCommandNetStandardInterfaceExecutor : DbCommandExecutor<IDbCommand>
	{
		public override string CommandTypeName => nameof(IDbCommand) + "-NetStandard";

		public override bool SupportsAsyncMethods => false;

		public override void ExecuteNonQuery(IDbCommand command) => command.ExecuteNonQuery();

		public override Task ExecuteNonQueryAsync(IDbCommand command) => Task.CompletedTask;

		public override Task ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

		public override void ExecuteScalar(IDbCommand command) => command.ExecuteScalar();

		public override Task ExecuteScalarAsync(IDbCommand command) => Task.CompletedTask;

		public override Task ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

		public override void ExecuteReader(IDbCommand command)
		{
			using var reader = command.ExecuteReader();
		}

		public override void ExecuteReader(IDbCommand command, CommandBehavior behavior)
		{
			using var reader = command.ExecuteReader(behavior);
		}

		public override Task ExecuteReaderAsync(IDbCommand command) => Task.CompletedTask;

		public override Task ExecuteReaderAsync(IDbCommand command, CommandBehavior behavior) => Task.CompletedTask;

		public override Task ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

		public override Task ExecuteReaderAsync(IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
