using System;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model
{
	internal static class DbSpanCommon
	{
		internal static Span StartSpan(IApmAgent agent, IDbCommand dbCommand) =>
			(Span)ExecutionSegmentCommon.GetCurrentExecutionSegment(agent).StartSpan(dbCommand.CommandText.Replace(Environment.NewLine, " ")
				, ApiConstants.TypeDb);

		internal static void EndSpan(Span span, IDbCommand dbCommand, TimeSpan? duration = null)
		{
			if (span.ShouldBeSentToApmServer)
			{
				span.Context.Db = new Database
				{
					Statement = dbCommand.CommandText.Replace(Environment.NewLine, " "),
					Instance = dbCommand.Connection.Database,
					Type = Database.TypeSql
				};
			}

			if (duration.HasValue) span.Duration = duration.Value.TotalMilliseconds;

			var providerType = dbCommand.Connection.GetType().FullName;

			switch (providerType)
			{
				case string str when str.ContainsOrdinalIgnoreCase("Sqlite"):
					span.Subtype = ApiConstants.SubtypeSqLite;
					break;
				case string str when str.ContainsOrdinalIgnoreCase("SqlConnection"):
					span.Subtype = ApiConstants.SubtypeMssql;
					break;
				default:
					span.Subtype = providerType; //TODO, TBD: this is an unknown provider
					break;
			}

			switch (dbCommand.CommandType)
			{
				case CommandType.Text:
					span.Action = ApiConstants.ActionQuery;
					break;
				case CommandType.StoredProcedure:
					span.Action = ApiConstants.ActionExec;
					break;
				case CommandType.TableDirect:
					span.Action = "tabledirect";
					break;
				default:
					span.Action = dbCommand.CommandType.ToString();
					break;
			}

			span.End();
		}
	}
}
