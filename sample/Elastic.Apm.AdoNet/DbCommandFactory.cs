// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Data;

namespace Elastic.Apm.AdoNet
{
	public class DbCommandFactory
	{
		private readonly IDbConnection _connection;
		private readonly string _tableName;

		public DbCommandFactory(IDbConnection connection, string tableName)
		{
			_connection = connection;
			_tableName = tableName;
		}

		public virtual IDbCommand GetCreateTableCommand()
        {
            var command = _connection.CreateCommand();
            command.CommandText = $"DROP TABLE IF EXISTS {_tableName}; CREATE TABLE {_tableName} (Id int PRIMARY KEY, Name varchar(100));";
            return command;
        }

        public virtual IDbCommand GetInsertRowCommand()
        {
            var command = _connection.CreateCommand();
            command.CommandText = $"INSERT INTO {_tableName} (Id, Name) VALUES (@Id, @Name);";
            command.AddParameterWithValue("Id", 1);
            command.AddParameterWithValue("Name", "Name1");
            return command;
        }

        public virtual IDbCommand GetUpdateRowCommand()
        {
            var command = _connection.CreateCommand();
            command.CommandText = $"UPDATE {_tableName} SET Name=@Name WHERE Id=@Id;";
			command.AddParameterWithValue("Name", "Name2");
            command.AddParameterWithValue("Id", 1);
            return command;
        }

        public virtual IDbCommand GetSelectScalarCommand()
        {
            var command = _connection.CreateCommand();
            command.CommandText = $"SELECT Name FROM {_tableName} WHERE Id=@Id;";
            command.AddParameterWithValue("Id", 1);
            return command;
        }

        public virtual IDbCommand GetSelectRowCommand()
        {
            var command = _connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {_tableName} WHERE Id=@Id;";
            command.AddParameterWithValue("Id", 1);
            return command;
        }

        public virtual IDbCommand GetDeleteRowCommand()
        {
            var command = _connection.CreateCommand();
            command.CommandText = $"DELETE FROM {_tableName} WHERE Id=@Id;";
            command.AddParameterWithValue("Id", 1);
            return command;
        }
	}
}
