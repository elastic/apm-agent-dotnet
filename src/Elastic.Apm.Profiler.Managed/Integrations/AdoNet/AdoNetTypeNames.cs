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

	internal class InstrumentMicrosoftDataSqliteAttribute : InstrumentAttribute
	{
		public InstrumentMicrosoftDataSqliteAttribute()
		{
			Assembly = "Microsoft.Data.Sqlite";
			Type = "Microsoft.Data.Sqlite.SqliteCommand";
			MinimumVersion = "2.0.0";
			MaximumVersion = "5.*.*";
			Group = "SqliteCommand";
		}
	}

	internal class InstrumentSystemDataSqliteAttribute : InstrumentAttribute
	{
		public InstrumentSystemDataSqliteAttribute()
		{
			Assembly = "System.Data.SQLite";
			Type = "System.Data.SQLite.SQLiteCommand";
			MinimumVersion = "1.0.0";
			MaximumVersion = "2.*.*";
			Group = "SqliteCommand";
		}
	}

	internal class InstrumentSystemDataSqlAttribute : InstrumentAttribute
	{
		public InstrumentSystemDataSqlAttribute()
		{
			Assembly = "System.Data";
			Type = "System.Data.SqlClient.SqlCommand";
			MinimumVersion = "4.0.0";
			MaximumVersion = "4.*.*";
			Group = "SqlCommand";
		}
	}

	internal class InstrumentSystemDataSqlClientAttribute : InstrumentAttribute
	{
		public InstrumentSystemDataSqlClientAttribute()
		{
			Assembly = "System.Data.SqlClient";
			Type = "System.Data.SqlClient.SqlCommand";
			MinimumVersion = "4.0.0";
			MaximumVersion = "4.*.*";
			Group = "SqlCommand";
		}
	}

	internal class InstrumentMicrosoftDataSqlClientAttribute : InstrumentAttribute
	{
		public InstrumentMicrosoftDataSqlClientAttribute()
		{
			Assembly = "Microsoft.Data.SqlClient";
			Type = "Microsoft.Data.SqlClient.SqlCommand";
			MinimumVersion = "1.0.0";
			MaximumVersion = "2.*.*";
			Group = "SqlCommand";
		}
	}

	internal class InstrumentSystemDataAttribute : InstrumentAttribute
	{
		public InstrumentSystemDataAttribute()
		{
			Assembly = "System.Data";
			Type = "System.Data.Common.DbCommand";
			MinimumVersion = "4.0.0";
			MaximumVersion = "4.*.*";
			Group = "AdoNet";
		}
	}

	internal class InstrumentSystemDataCommonAttribute : InstrumentAttribute
	{
		public InstrumentSystemDataCommonAttribute()
		{
			Assembly = "System.Data.Common";
			Type = "System.Data.Common.DbCommand";
			MinimumVersion = "4.0.0";
			MaximumVersion = "5.*.*";
			Group = "AdoNet";
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

		internal static class MicrosoftDataSqlite
		{
			public const string DataReader = "Microsoft.Data.Sqlite.SqliteDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<Microsoft.Data.Sqlite.SqliteDataReader>";
		}

		internal static class SystemDataSqlite
		{
			public const string DataReader = "System.Data.SQLite.SQLiteDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<System.Data.SQLite.SQLiteDataReader>";
		}

		internal static class SystemDataSqlServer
		{
			public const string DataReader = "System.Data.SqlClient.SqlDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<System.Data.SqlClient.SqlDataReader>";
		}

		internal static class MicrosoftDataSqlServer
		{
			public const string DataReader = "Microsoft.Data.SqlClient.SqlDataReader";
			public const string TaskDataReader = "System.Threading.Tasks.Task`1<Microsoft.Data.SqlClient.SqlDataReader>";
		}
	}
}
