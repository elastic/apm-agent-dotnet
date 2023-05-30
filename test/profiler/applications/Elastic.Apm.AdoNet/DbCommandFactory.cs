// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet

using System.Data;

namespace Elastic.Apm.AdoNet
{
	public class DbCommandFactory
	{
		protected string TableName { get; set; }

		protected IDbConnection Connection { get; }

		public DbCommandFactory(IDbConnection connection, string tableName)
		{
			Connection = connection;
			TableName = tableName;
		}

		public virtual IDbCommand GetCreateTableCommand()
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"DROP TABLE IF EXISTS {TableName}; CREATE TABLE {TableName} (Id int PRIMARY KEY, Name varchar(100));";
            return command;
        }

        public virtual IDbCommand GetInsertRowCommand()
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"INSERT INTO {TableName} (Id, Name) VALUES (@Id, @Name);";
            command.AddParameterWithValue("Id", 1);
            command.AddParameterWithValue("Name", "Name1");
            return command;
        }

        public virtual IDbCommand GetUpdateRowCommand()
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"UPDATE {TableName} SET Name=@Name WHERE Id=@Id;";
			command.AddParameterWithValue("Name", "Name2");
            command.AddParameterWithValue("Id", 1);
            return command;
        }

        public virtual IDbCommand GetSelectScalarCommand()
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"SELECT Name FROM {TableName} WHERE Id=@Id;";
            command.AddParameterWithValue("Id", 1);
            return command;
        }

        public virtual IDbCommand GetSelectRowCommand()
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {TableName} WHERE Id=@Id;";
            command.AddParameterWithValue("Id", 1);
            return command;
        }

        public virtual IDbCommand GetDeleteRowCommand()
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"DELETE FROM {TableName} WHERE Id=@Id;";
            command.AddParameterWithValue("Id", 1);
            return command;
        }
	}
}
