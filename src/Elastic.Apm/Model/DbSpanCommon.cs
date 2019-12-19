using System;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Model
{
	internal static class DbSpanCommon
	{
		internal static class DefaultPorts
		{
			internal const int MsSql = 1433;
			internal const int Oracle = 1521;
			internal const int MySql = 3306;
			internal const int PostgreSql = 5432;
		}

		internal static Span StartSpan(IApmAgent agent, IDbCommand dbCommand) =>
			(Span)ExecutionSegmentCommon.GetCurrentExecutionSegment(agent).StartSpan(dbCommand.CommandText.Replace(Environment.NewLine, " ")
				, ApiConstants.TypeDb);

		internal static void EndSpan(Span span, IDbCommand dbCommand, IApmLogger logger, DbConnectionStringParser dbConnectionStringParser
			, TimeSpan? duration = null)
		{
			if (duration.HasValue) span.Duration = duration.Value.TotalMilliseconds;

			GetDefaultProperties(dbCommand.Connection.GetType().FullName, out var spanSubtype, out var isEmbeddedDb, out var defaultPort);
			span.Subtype = spanSubtype;
			span.Action = GetSpanAction(dbCommand.CommandType);

			if (span.ShouldBeSentToApmServer)
			{
				span.Context.Db = new Database
				{
					Statement = dbCommand.CommandText.Replace(Environment.NewLine, " "),
					Instance = dbCommand.Connection.Database,
					Type = Database.TypeSql
				};

				span.Context.Destination =
					GetDestination(dbCommand.Connection?.ConnectionString, isEmbeddedDb, defaultPort, dbConnectionStringParser);
			}

			span.End();
		}

		private static void GetDefaultProperties(string dbConnectionClassName, out string spanSubtype, out bool isEmbeddedDb, out int? defaultPort)
		{
			isEmbeddedDb = false;
			switch (dbConnectionClassName)
			{
				case string str when str.ContainsOrdinalIgnoreCase("SQLite"):
					spanSubtype = ApiConstants.SubtypeSqLite;
					isEmbeddedDb = true;
					defaultPort = null;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("MySQL"):
					spanSubtype = ApiConstants.SubtypeMySql;
					defaultPort = DefaultPorts.MySql;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("Oracle"):
					spanSubtype = ApiConstants.SubtypeOracle;
					defaultPort = DefaultPorts.Oracle;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("Postgre"):
					spanSubtype = ApiConstants.SubtypePostgreSql;
					defaultPort = DefaultPorts.PostgreSql;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("NpgSQL"):
					spanSubtype = ApiConstants.SubtypePostgreSql;
					defaultPort = DefaultPorts.PostgreSql;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("Microsoft"):
					spanSubtype = ApiConstants.SubtypeMssql;
					defaultPort = DefaultPorts.MsSql;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("System.Data.SqlClient"):
					spanSubtype = ApiConstants.SubtypeMssql;
					defaultPort = DefaultPorts.MsSql;
					break;
				default:
					spanSubtype = dbConnectionClassName; //TODO, TBD: this is an unknown provider
					defaultPort = null;
					break;
			}
		}

		private static string GetSpanAction(CommandType dbCommandType)
		{
			switch (dbCommandType)
			{
				case CommandType.Text:
					return ApiConstants.ActionQuery;
				case CommandType.StoredProcedure:
					return ApiConstants.ActionExec;
				case CommandType.TableDirect:
					return "tabledirect";
				default:
					return dbCommandType.ToString();
			}
		}

		private static Destination GetDestination(string dbConnectionString, bool isEmbeddedDb, int? defaultPort
			, DbConnectionStringParser dbConnectionStringParser)
		{
			if (isEmbeddedDb || dbConnectionString == null) return null;

			var destination = dbConnectionStringParser.TryExtractDestination(dbConnectionString);

			if (!destination.Port.HasValue) destination.Port = defaultPort;

			return destination;
		}
	}
}
