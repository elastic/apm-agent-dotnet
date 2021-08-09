// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.Profiler.Managed.Integrations.AdoNet
{
	internal static class DbSpanFactory<T>
	{
		private static readonly Type _type;
		private static readonly string _fullName;
		private static readonly InstrumentationFlag _instrumentationFlag;

		static DbSpanFactory()
		{
			_type = typeof(T);
			_fullName = _type.FullName;
			_instrumentationFlag = GetInstrumentationFlag(_fullName);
		}

		internal static ISpan CreateSpan(ApmAgent agent, IDbCommand command)
		{
			var transaction = agent.Tracer.CurrentTransaction;

			if (transaction is null)
				return null;

			// if the current execution segment is already for this instrumentation,
			// skip creating another db span for it, to prevent instrumenting delegated methods.
			if (agent.GetCurrentExecutionSegment() is Span span && span.InstrumentationFlag.HasFlag(_instrumentationFlag))
				return null;

			return agent.TracerInternal.DbSpanCommon.StartSpan(Agent.Instance, command, _instrumentationFlag);
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
				case { } str when str.ContainsOrdinalIgnoreCase("SQLite"):
					return InstrumentationFlag.Sqlite;
				case { } str when str.ContainsOrdinalIgnoreCase("MySQL"):
					return InstrumentationFlag.MySql;
				case { } when typeName.ContainsOrdinalIgnoreCase("Oracle"):
					return InstrumentationFlag.Oracle;
				case { } when typeName.ContainsOrdinalIgnoreCase("Postgre"):
				case { } when typeName.ContainsOrdinalIgnoreCase("NpgSQL"):
					return InstrumentationFlag.Postgres;
				case { } when typeName.ContainsOrdinalIgnoreCase("Microsoft"):
				case { } when typeName.ContainsOrdinalIgnoreCase("System.Data.SqlClient"):
					return InstrumentationFlag.SqlClient;
				default:
					return InstrumentationFlag.None;
			}
		}
	}
}
