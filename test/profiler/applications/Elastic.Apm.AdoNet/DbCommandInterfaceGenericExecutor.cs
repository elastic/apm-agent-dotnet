// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet

using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet.NetStandard;

namespace Elastic.Apm.AdoNet
{
	public class DbCommandInterfaceGenericExecutor<TCommand> : DbCommandExecutor<TCommand>
		where TCommand : IDbCommand
	{
		public override string CommandTypeName => "IDbCommandGenericConstraint<" + typeof(TCommand).Name + ">";

		public override bool SupportsAsyncMethods => false;

		public override void ExecuteNonQuery(TCommand command) => command.ExecuteNonQuery();

		public override Task ExecuteNonQueryAsync(TCommand command) => Task.CompletedTask;

		public override Task ExecuteNonQueryAsync(TCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

		public override void ExecuteScalar(TCommand command) => command.ExecuteScalar();

		public override Task ExecuteScalarAsync(TCommand command) => Task.CompletedTask;

		public override Task ExecuteScalarAsync(TCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

		public override void ExecuteReader(TCommand command)
		{
			using var reader = command.ExecuteReader();
		}

		public override void ExecuteReader(TCommand command, CommandBehavior behavior)
		{
			using var reader = command.ExecuteReader(behavior);
		}

		public override Task ExecuteReaderAsync(TCommand command) => Task.CompletedTask;

		public override Task ExecuteReaderAsync(TCommand command, CommandBehavior behavior) => Task.CompletedTask;

		public override Task ExecuteReaderAsync(TCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

		public override Task ExecuteReaderAsync(TCommand command, CommandBehavior behavior, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
