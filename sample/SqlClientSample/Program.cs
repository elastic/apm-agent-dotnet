// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet;

namespace SqlClientSample
{
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var connectionType = typeof(SqlConnection);
			var guid = Guid.NewGuid().ToString("N");

			Console.WriteLine($"Run commands ({guid})");

			using (var connection = CreateAndOpenConnection(connectionType))
			{
				var dbCommandFactory = new DbCommandFactory(connection, $"sqlserver_all_{guid}");

				await DbCommandRunner.RunAllAsync<SqlCommand>(
					dbCommandFactory,
					new SqlCommandExecutor(),
					cancellationTokenSource.Token);
			}

			var sqlAssembly = Assembly.LoadFile(connectionType.Assembly.Location);
			var sqlConnection = sqlAssembly.GetType(connectionType.FullName);

			using (var connection = CreateAndOpenConnection(sqlConnection))
			{
				var dbCommandFactory = new DbCommandFactory(connection, $"sqlserver_basetypes_{guid}");

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
			var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING");
			var connection = Activator.CreateInstance(connectionType, connectionString) as DbConnection;
			connection.Open();
			return connection;
		}
	}
}
