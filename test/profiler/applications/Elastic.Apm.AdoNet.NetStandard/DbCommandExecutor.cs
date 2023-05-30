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

namespace Elastic.Apm.AdoNet.NetStandard
{
	public abstract class DbCommandExecutor<TDbCommand> : IDbCommandExecutor
		where TDbCommand : IDbCommand
	{
		public abstract string CommandTypeName { get; }

		public abstract bool SupportsAsyncMethods { get; }

		public abstract void ExecuteNonQuery(TDbCommand command);
		public abstract Task ExecuteNonQueryAsync(TDbCommand command);
		public abstract Task ExecuteNonQueryAsync(TDbCommand command, CancellationToken cancellationToken);
		public abstract void ExecuteScalar(TDbCommand command);
		public abstract Task ExecuteScalarAsync(TDbCommand command);
		public abstract Task ExecuteScalarAsync(TDbCommand command, CancellationToken cancellationToken);
		public abstract void ExecuteReader(TDbCommand command);
		public abstract void ExecuteReader(TDbCommand command, CommandBehavior behavior);
		public abstract Task ExecuteReaderAsync(TDbCommand command);
		public abstract Task ExecuteReaderAsync(TDbCommand command, CommandBehavior behavior);
		public abstract Task ExecuteReaderAsync(TDbCommand command, CancellationToken cancellationToken);
		public abstract Task ExecuteReaderAsync(TDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken);

		void IDbCommandExecutor.ExecuteNonQuery(IDbCommand command) => ExecuteNonQuery((TDbCommand)command);
		Task IDbCommandExecutor.ExecuteNonQueryAsync(IDbCommand command) => ExecuteNonQueryAsync((TDbCommand)command);
		Task IDbCommandExecutor.ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken) => ExecuteNonQueryAsync((TDbCommand)command, cancellationToken);
		void IDbCommandExecutor.ExecuteScalar(IDbCommand command) => ExecuteScalar((TDbCommand)command);
		Task IDbCommandExecutor.ExecuteScalarAsync(IDbCommand command) => ExecuteScalarAsync((TDbCommand)command);
		Task IDbCommandExecutor.ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken) => ExecuteScalarAsync((TDbCommand)command, cancellationToken);
		void IDbCommandExecutor.ExecuteReader(IDbCommand command) => ExecuteReader((TDbCommand)command);
		void IDbCommandExecutor.ExecuteReader(IDbCommand command, CommandBehavior behavior) => ExecuteReader((TDbCommand)command, behavior);
		Task IDbCommandExecutor.ExecuteReaderAsync(IDbCommand command) => ExecuteReaderAsync((TDbCommand)command);
		Task IDbCommandExecutor.ExecuteReaderAsync(IDbCommand command, CommandBehavior behavior) => ExecuteReaderAsync((TDbCommand)command, behavior);
		Task IDbCommandExecutor.ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken) => ExecuteReaderAsync((TDbCommand)command, cancellationToken);
		Task IDbCommandExecutor.ExecuteReaderAsync(IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken) => ExecuteReaderAsync((TDbCommand)command, behavior, cancellationToken);
	}
}
