// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet;
using Oracle.ManagedDataAccess.Client;

namespace OracleManagedDataAccessSample
{
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var connectionType = typeof(OracleConnection);
			var guid = Guid.NewGuid().ToString("N").Substring(0,8);

			Console.WriteLine($"Run commands ({guid})");

			using (var connection = CreateAndOpenConnection(connectionType))
			{
				var dbCommandFactory = new OracleDbCommandFactory(connection, $"oracle_all_{guid}");

				await DbCommandRunner.RunAllAsync<OracleCommand>(
					dbCommandFactory,
					new OracleCommandExecutor(),
					cancellationTokenSource.Token);
			}

			var oracleAssembly = Assembly.LoadFile(connectionType.Assembly.Location);
			var oracleConnection = oracleAssembly.GetType(connectionType.FullName);

			using (var connection = CreateAndOpenConnection(oracleConnection))
			{
				var dbCommandFactory = new OracleDbCommandFactory(connection, $"oracle_basetypes_{guid}");

				await DbCommandRunner.RunBaseTypesAsync(
					dbCommandFactory,
					cancellationTokenSource.Token);
			}

			Console.WriteLine("Finished sending commands");

			// allow the agent time to send the spans
			await Task.Delay(TimeSpan.FromSeconds(40), cancellationTokenSource.Token);
			return 0;
		}

		private static DbConnection CreateAndOpenConnection(Type connectionType)
		{
			var connectionString = Environment.GetEnvironmentVariable("ORACLE_CONNECTION_STRING");
			var connection = Activator.CreateInstance(connectionType, connectionString) as DbConnection;
			connection.Open();
			return connection;
		}
	}
}
