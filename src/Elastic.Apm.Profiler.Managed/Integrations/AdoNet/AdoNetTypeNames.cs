// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Profiler.Managed.Core;

namespace Elastic.Apm.Profiler.Managed.Integrations.AdoNet
{
	internal class InstrumentMySqlAttribute : InstrumentAttribute
	{
		public InstrumentMySqlAttribute()
		{
			Assembly = "MySql.Data";
			Type = "MySql.Data.MySqlClient.MySqlCommand";
			MinimumVersion = "6.7.0";
			MaximumVersion = "8.*.*";
			Group = "MySqlCommand";
		}
	}

	internal class InstrumentNpgsqlAttribute : InstrumentAttribute
	{
		public InstrumentNpgsqlAttribute()
		{
			Assembly = "Npgsql";
			Type = "Npgsql.NpgsqlCommand";
			MinimumVersion = "4.0.0";
			MaximumVersion = "5.*.*";
			Group = "NpgsqlCommand";
		}
	}

	internal class InstrumentOracleManagedDataAccessAttribute : InstrumentAttribute
	{
		public InstrumentOracleManagedDataAccessAttribute()
		{
			Assembly = "Oracle.ManagedDataAccess";
			Type = "Oracle.ManagedDataAccess.Client.OracleCommand";
			MinimumVersion = "4.122.0";
			MaximumVersion = "4.122.*";
			Group = "OracleCommand";
		}
	}

	internal class InstrumentOracleManagedDataAccessCoreAttribute : InstrumentAttribute
	{
		public InstrumentOracleManagedDataAccessCoreAttribute()
		{
			Assembly = "Oracle.ManagedDataAccess";
			Type = "Oracle.ManagedDataAccess.Client.OracleCommand";
			MinimumVersion = "2.0.0";
			MaximumVersion = "2.*.*";
			Group = "OracleCommand";
		}
	}

	internal class InstrumentSqliteAttribute : InstrumentAttribute
	{
		public InstrumentSqliteAttribute()
		{
			Assembly = "Microsoft.Data.Sqlite";
			Type = "Microsoft.Data.Sqlite.SqliteCommand";
			MinimumVersion = "2.0.0";
			MaximumVersion = "5.*.*";
			Group = "SqliteCommand";
		}
	}

	internal class AdoNetTypeNames
	{
		public const string CommandBehavior = "System.Data.CommandBehavior";
		public const string DbDataReader = "System.Data.Common.DbDataReader";
		public const string TaskDbDataReader = "System.Threading.Tasks.Task`1<System.Data.Common.DbDataReader>";
		public const string TaskInt32 = "System.Threading.Tasks.Task`1<System.Int32>";
		public const string TaskObject = "System.Threading.Tasks.Task`1<System.Object>";

		public const string ExecuteNonQuery = nameof(ExecuteNonQuery);
		public const string ExecuteNonQueryAsync = nameof(ExecuteNonQueryAsync);
		public const string ExecuteScalar = nameof(ExecuteScalar);
		public const string ExecuteScalarAsync = nameof(ExecuteScalarAsync);
		public const string ExecuteReader = nameof(ExecuteReader);
		public const string ExecuteReaderAsync = nameof(ExecuteReaderAsync);
		public const string ExecuteDbDataReader = nameof(ExecuteDbDataReader);
		public const string ExecuteDbDataReaderAsync = nameof(ExecuteDbDataReaderAsync);

		internal static class MySql
		{
			public const string DataReader = "MySql.Data.MySqlClient.MySqlDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<MySql.Data.MySqlClient.MySqlDataReader>";
		}

		internal static class Npgsql
		{
			public const string DataReader = "Npgsql.NpgsqlDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<Npgsql.NpgsqlDataReader>";
		}

		internal static class OracleManagedDataAccess
		{
			public const string DataReader = "Oracle.ManagedDataAccess.Client.OracleDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<Oracle.ManagedDataAccess.Client.OracleDataReader>";
		}

		internal static class Sqlite
		{
			public const string DataReader = "Microsoft.Data.Sqlite.SqliteDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<Microsoft.Data.Sqlite.SqliteDataReader>";
		}
	}
}
