// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using static Elastic.Apm.Model.InstrumentationFlag;

namespace Elastic.Apm.Profiler.Managed.Integrations.AdoNet
{
	internal static class DbSpanFactory<T>
	{
		private static readonly InstrumentationFlag _instrumentationFlag;

		static DbSpanFactory()
		{
			var type = typeof(T);
			_instrumentationFlag = GetInstrumentationFlag(type.FullName);
		}

		internal static ISpan CreateSpan(ApmAgent agent, IDbCommand command)
		{
			if (agent.Tracer.CurrentTransaction is null)
				return null;

			// if the current execution segment is
			// 1. already for this instrumentation or instrumentation is AdoNet (System.Data.Common.DbCommand) and the type is "db"
			// and
			// 2. for the same command text
			// skip creating another db span for it, to prevent instrumenting delegated methods.
			if (agent.GetCurrentExecutionSegment() is Span span &&
				(span.InstrumentationFlag.HasFlag(_instrumentationFlag) ||
					span.Type == ApiConstants.TypeDb &&
						(_instrumentationFlag == InstrumentationFlag.AdoNet || span.InstrumentationFlag == InstrumentationFlag.AdoNet)) &&
				span.Name == DbSpanCommon.GetDbSpanName(command))
				return null;

			return agent.TracerInternal.DbSpanCommon.StartSpan(agent, command, _instrumentationFlag);
		}

		internal static void EndSpan(ApmAgent agent, IDbCommand command, ISpan span, Exception exception)
		{
			if (span != null)
			{
				var outcome = Outcome.Success;
				if (exception != null)
				{
					span.CaptureException(exception);
					outcome = Outcome.Failure;
				}

				agent.TracerInternal.DbSpanCommon.EndSpan(span, command, outcome);
			}
		}

		private static InstrumentationFlag GetInstrumentationFlag(string typeName)
		{
			switch (typeName)
			{
				case { } str when str.ContainsOrdinalIgnoreCase("Sqlite"):
					return Sqlite;
				case { } str when str.ContainsOrdinalIgnoreCase("MySQL"):
					return MySql;
				case { } when typeName.ContainsOrdinalIgnoreCase("Oracle"):
					return Oracle;
				case { } when typeName.ContainsOrdinalIgnoreCase("Postgre"):
				case { } when typeName.ContainsOrdinalIgnoreCase("NpgSql"):
					return Postgres;
				case { } when typeName.ContainsOrdinalIgnoreCase("Microsoft"):
				case { } when typeName.ContainsOrdinalIgnoreCase("System.Data.SqlClient"):
					return SqlClient;
				case { } when typeName.ContainsOrdinalIgnoreCase("System.Data.Common"):
					return InstrumentationFlag.AdoNet;
				default:
					return None;
			}
		}
	}
}
