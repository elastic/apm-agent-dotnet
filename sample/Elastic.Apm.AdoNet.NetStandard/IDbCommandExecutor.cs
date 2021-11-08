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
	public interface IDbCommandExecutor
	{
		string CommandTypeName { get; }
		bool SupportsAsyncMethods { get; }

		void ExecuteNonQuery(IDbCommand command);
		Task ExecuteNonQueryAsync(IDbCommand command);
		Task ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken);

		void ExecuteScalar(IDbCommand command);
		Task ExecuteScalarAsync(IDbCommand command);
		Task ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken);

		void ExecuteReader(IDbCommand command);
		void ExecuteReader(IDbCommand command, CommandBehavior behavior);
		Task ExecuteReaderAsync(IDbCommand command);
		Task ExecuteReaderAsync(IDbCommand command, CommandBehavior behavior);
		Task ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken);
		Task ExecuteReaderAsync(IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken);
	}
}
