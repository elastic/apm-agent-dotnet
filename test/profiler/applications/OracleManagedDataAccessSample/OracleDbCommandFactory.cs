// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Data;
using Elastic.Apm.AdoNet;

namespace OracleManagedDataAccessSample
{
	public class OracleDbCommandFactory : DbCommandFactory
	{
		private int _count = 0;
		private readonly string _tableName;

		public OracleDbCommandFactory(IDbConnection connection, string tableName)
			: base(connection, tableName) =>
			_tableName = tableName;

		public override IDbCommand GetCreateTableCommand()
		{
			var command = Connection.CreateCommand();
			command.CommandText = $"CREATE TABLE {TableName} (Id number(10) not null, Name varchar2(100) not null)";
			return command;
		}

		public override IDbCommand GetInsertRowCommand()
		{
			var command = Connection.CreateCommand();
			command.CommandText = $"INSERT INTO {TableName} (Id, Name) VALUES (:Id, :Name)";
			command.AddParameterWithValue("Id", 1);
			command.AddParameterWithValue("Name", "Name1");
			return command;
		}

		public override IDbCommand GetSelectScalarCommand()
		{
			var command = Connection.CreateCommand();
			command.CommandText = $"SELECT Name FROM {TableName} WHERE Id=:Id";
			command.AddParameterWithValue("Id", 1);
			return command;
		}

		public override IDbCommand GetUpdateRowCommand()
		{
			var command = Connection.CreateCommand();
			command.CommandText = $"UPDATE {TableName} SET Name=:Name WHERE Id=:Id";
			command.AddParameterWithValue("Name", "Name2");
			command.AddParameterWithValue("Id", 1);
			return command;
		}

		public override IDbCommand GetSelectRowCommand()
		{
			var command = Connection.CreateCommand();
			command.CommandText = $"SELECT * FROM {TableName} WHERE Id=:Id";
			command.AddParameterWithValue("Id", 1);
			return command;
		}

		public override IDbCommand GetDeleteRowCommand()
		{
			var command = Connection.CreateCommand();
			command.CommandText = $"DELETE FROM {TableName} WHERE Id=:Id";
			command.AddParameterWithValue("Id", 1);
			TableName = _tableName + _count++;
			return command;
		}
	}
}
