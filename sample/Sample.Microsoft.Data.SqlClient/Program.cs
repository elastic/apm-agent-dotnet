using Elastic.Apm;
using Elastic.Apm.Logging;
using Elastic.Apm.SqlClient;
using Elastic.Apm.Tests.Utilities;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

var agentConfig = new MockConfiguration(centralConfig: "false");
Agent.Setup(new AgentComponents(ConsoleLogger.LoggerOrDefault(LogLevel.Trace), agentConfig));
Agent.Subscribe(new SqlClientDiagnosticSubscriber());
using var instance = Agent.Instance;

Console.WriteLine("Starting mssql container");
await using var msSql = new MsSqlBuilder().Build();
await msSql.StartAsync();
var connectionString = msSql.GetConnectionString();

Console.WriteLine("Performing database calls");

var transaction = Agent.Tracer.StartTransaction("test-sql-client-calls", "app");
DoSqlClientCall(connectionString);
transaction.End();

Console.WriteLine("Done!");

static void DoSqlClientCall(string connectionString)
{
	using var connection = new SqlConnection(connectionString);
	connection.Open();

	var create = new SqlCommand("CREATE TABLE Emails(Id INTEGER,Email TEXT)", connection);
	create.ExecuteNonQuery();

	InsertEmail("test1@example.example", connection);
	InsertEmail("test2@example.example", connection);

	var command = new SqlCommand("SELECT Email FROM Emails", connection);
	var reader = command.ExecuteReader();
	try
	{
		while (reader.Read())
		{
			var read = reader["Email"];
			Console.WriteLine($"{read}");
		}
	}
	finally
	{
		reader.Close();
	}
}

static void InsertEmail(string email, SqlConnection connection)
{
	using var command = connection.CreateCommand();
	command.CommandText = "INSERT into Emails(id ,email) values(@id,@email)";
	command.Prepare();
	command.Parameters.AddWithValue("@id", 0);
	command.Parameters.AddWithValue("@email", email);
	command.ExecuteNonQuery();
}

