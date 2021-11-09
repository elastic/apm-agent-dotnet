﻿// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.AdoNet;
using MySql.Data.MySqlClient;

namespace MySqlDataSample
{
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var connectionType = typeof(MySqlConnection);
			var guid = Guid.NewGuid().ToString("N");

			Console.WriteLine($"Run commands ({guid})");

			using (var connection = CreateAndOpenConnection(connectionType))
			{
				var dbCommandFactory = new DbCommandFactory(connection, $"mysql_all_{guid}");

				await DbCommandRunner.RunAllAsync<MySqlCommand>(
					dbCommandFactory,
					new MySqlCommandExecutor(),
					cancellationTokenSource.Token);
			}

			var npgsqlAssembly = Assembly.LoadFile(connectionType.Assembly.Location);
			var npgsqlConnection = npgsqlAssembly.GetType(connectionType.FullName);

			using (var connection = CreateAndOpenConnection(npgsqlConnection))
			{
				var dbCommandFactory = new DbCommandFactory(connection, $"mysql_basetypes_{guid}");

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
			var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
			var connection = Activator.CreateInstance(connectionType, connectionString) as DbConnection;
			connection.Open();
			return connection;
		}
	}
}
