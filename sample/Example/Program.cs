using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Databases;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.Modules.Databases;
using DotNet.Testcontainers.Containers.WaitStrategies;
using Elastic.Apm;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Example
{
	internal static class Program
    {
		private const string PostgresUserName = "postgres";
		private const string PostgresPassword = "postgres";
		private const string MySQLUserName = "mysql";
		private const string MySQLPassword = "mysqlpassword";


		private static async Task<int> Main(string[] args)
		{
			var postgresBuilder = new TestcontainersBuilder<PostgreSqlTestcontainer>()
				.WithDatabase(new PostgreSqlTestcontainerConfiguration
				{
					Database = "db",
					Username = PostgresUserName,
					Password = PostgresPassword,
				})
				.WithPortBinding(5432,5432);

			var mySqlBuilder = new TestcontainersBuilder<MySqlTestcontainer>()
				.WithDatabase(new MySqlTestcontainerConfiguration
				{
					Database = "db",
					Username = MySQLUserName,
					Password = MySQLPassword,
				})
				.WithPortBinding(3306,3306);

			await using (var postgres = postgresBuilder.Build())
			await using (var mySql = mySqlBuilder.Build())
			{
				await postgres.StartAsync();
				await mySql.StartAsync();

				Agent.Tracer.CaptureTransaction("Demo", "example", () =>
				{
					CallSqlite();
					CallMySql();
					CallPostgres();
				});
			}

			await Task.Delay(TimeSpan.FromSeconds(12));
			return 0;
		}

		private static void CallSqlite()
		{
			var connectionString = "Data Source=:memory:";
			using var connection = new SqliteConnection(connectionString);
			connection.Open();

			using (var command = connection.CreateCommand())
			{
				command.CommandText =
					"CREATE TABLE Message (Text TEXT);" +
					"INSERT INTO Message (Text) VALUES ('Came from sqlite');";
				command.ExecuteNonQuery();
			}

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT Text FROM Message;";
				var message = command.ExecuteScalar() as string;
				Console.WriteLine("Response from Sqlite:");
				Console.WriteLine(message);
				Console.WriteLine();
			}
		}

		private static void CallMySql()
		{
			try
			{
				var connectionString = $"server=127.0.0.1;uid={MySQLUserName};pwd={MySQLPassword};";
				using var connection = new MySqlConnection(connectionString);
				connection.Open();

				using var command = connection.CreateCommand();
				command.CommandText =
					"SHOW VARIABLES LIKE '%version%'";

				// execute command
				var reader = command.ExecuteReader();

				var builder = new StringBuilder();
				while (reader.Read())
				{
					builder.AppendLine($"{reader["Variable_name"]}:{reader["Value"]}");
				}

				Console.WriteLine("Response from MySql:");
				Console.WriteLine(builder.ToString());
				Console.WriteLine();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private static void CallPostgres()
		{
			try
			{
				var connectionString = $"server=127.0.0.1;uid={PostgresUserName};pwd={PostgresPassword};";
				using var connection = new NpgsqlConnection(connectionString);
				connection.Open();

				using var command = connection.CreateCommand();
				command.CommandText = "SELECT version();";

				// execute command
				var reader = command.ExecuteReader();

				var builder = new StringBuilder();
				while (reader.Read())
					builder.AppendLine($"{reader["version"]}");

				Console.WriteLine("Response from Postgres:");
				Console.WriteLine(builder.ToString());
				Console.WriteLine();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}
