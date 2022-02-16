// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data;
using System.Text;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Model
{
	internal class DbSpanCommon
	{
		private readonly DbConnectionStringParser _dbConnectionStringParser;

		internal DbSpanCommon(IApmLogger logger) => _dbConnectionStringParser = new DbConnectionStringParser(logger);

		internal static class DefaultPorts
		{
			internal const int MsSql = 1433;
			internal const int MySql = 3306;
			internal const int Oracle = 1521;
			internal const int PostgreSql = 5432;
		}

		internal ISpan StartSpan(IApmAgent agent, IDbCommand dbCommand, InstrumentationFlag instrumentationFlag, string subType = null,
			bool captureStackTraceOnStart = false
		)
		{
			var spanName = GetDbSpanName(dbCommand);
			return ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(agent, spanName, ApiConstants.TypeDb, subType, instrumentationFlag,
				captureStackTraceOnStart, true);
		}

		internal static string GetDbSpanName(IDbCommand dbCommand)
		{
			var signatureParser =  new SignatureParser(new Scanner());
			var name = new StringBuilder();
			signatureParser.QuerySignature(dbCommand.CommandText.Replace(Environment.NewLine, " "), name,  preparedStatement: dbCommand.Parameters.Count > 0);
			return name.ToString();
			//return dbCommand.CommandText.Replace(Environment.NewLine, " ");
		}

		internal void EndSpan(ISpan span, IDbCommand dbCommand, Outcome outcome, TimeSpan? duration = null)
		{
			if (span is Span capturedSpan)
			{
				if (duration.HasValue) capturedSpan.Duration = duration.Value.TotalMilliseconds;

				GetDefaultProperties(dbCommand.Connection.GetType().FullName, out var spanSubtype, out var defaultPort);
				capturedSpan.Subtype = spanSubtype;
				capturedSpan.Action = GetSpanAction(dbCommand.CommandType);

				if (capturedSpan.ShouldBeSentToApmServer)
				{
					capturedSpan.Context.Db = new Database
					{
						Statement = dbCommand.CommandText.Replace(Environment.NewLine, " "), Instance = dbCommand.Connection.Database, Type = Database.TypeSql
					};

					capturedSpan.Context.Destination = GetDestination(dbCommand.Connection?.ConnectionString, defaultPort);
				}
				else
					capturedSpan.ServiceResource =  !string.IsNullOrEmpty(capturedSpan.Subtype) ? capturedSpan.Subtype : Database.TypeSql + dbCommand.Connection.Database;

				capturedSpan.Outcome = outcome;
			}
			span.End();
		}

		private static void GetDefaultProperties(string dbConnectionClassName, out string spanSubtype, out int? defaultPort)
		{
			switch (dbConnectionClassName)
			{
				case { } str when str.ContainsOrdinalIgnoreCase("SQLite"):
					spanSubtype = ApiConstants.SubtypeSqLite;
					defaultPort = null;
					break;
				case { } str when str.ContainsOrdinalIgnoreCase("MySQL"):
					spanSubtype = ApiConstants.SubtypeMySql;
					defaultPort = DefaultPorts.MySql;
					break;
				case { } str when str.ContainsOrdinalIgnoreCase("Oracle"):
					spanSubtype = ApiConstants.SubtypeOracle;
					defaultPort = DefaultPorts.Oracle;
					break;
				case { } str when str.ContainsOrdinalIgnoreCase("Postgre"):
					spanSubtype = ApiConstants.SubtypePostgreSql;
					defaultPort = DefaultPorts.PostgreSql;
					break;
				case { } str when str.ContainsOrdinalIgnoreCase("NpgSQL"):
					spanSubtype = ApiConstants.SubtypePostgreSql;
					defaultPort = DefaultPorts.PostgreSql;
					break;
				case { } str when str.ContainsOrdinalIgnoreCase("Microsoft"):
					spanSubtype = ApiConstants.SubtypeMssql;
					defaultPort = DefaultPorts.MsSql;
					break;
				case { } str when str.ContainsOrdinalIgnoreCase("System.Data.SqlClient"):
					spanSubtype = ApiConstants.SubtypeMssql;
					defaultPort = DefaultPorts.MsSql;
					break;
				default:
					spanSubtype = dbConnectionClassName; //TODO, TBD: this is an unknown provider
					defaultPort = null;
					break;
			}
		}

		private static string GetSpanAction(CommandType dbCommandType) =>
			dbCommandType switch
			{
				CommandType.Text => ApiConstants.ActionQuery,
				CommandType.StoredProcedure => ApiConstants.ActionExec,
				CommandType.TableDirect => "tabledirect",
				_ => dbCommandType.ToString()
			};

		internal Destination GetDestination(string dbConnectionString, int? defaultPort)
		{
			if (dbConnectionString == null) return null;

			var destination = _dbConnectionStringParser.ExtractDestination(dbConnectionString);
			if (destination == null) return null;

			if (!destination.Port.HasValue) destination.Port = defaultPort;

			return destination;
		}
	}
}
