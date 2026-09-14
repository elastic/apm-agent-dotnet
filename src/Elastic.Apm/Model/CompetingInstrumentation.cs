// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Data;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Detects whether the database command an instrumentation module is about to trace is already traced by a competing
	/// module. Competing modules keep their span current while the command executes, so the current span identifies them.
	/// </summary>
	internal static class CompetingInstrumentation
	{
		/// <summary>
		/// Entity Framework Core and Entity Framework 6, which trace the command they hand to ADO.NET.
		/// </summary>
		private const InstrumentationFlag EntityFramework = InstrumentationFlag.EfCore | InstrumentationFlag.EfClassic;

		/// <summary>
		/// The profiler's ADO.NET CallTarget integrations, which flag their spans as
		/// <see cref="InstrumentationFlag.SqlClient" /> (SqlCommand) or <see cref="InstrumentationFlag.AdoNet" /> (DbCommand).
		/// Neither of the SqlClient listeners makes its own spans current, so a current span carrying one of these flags
		/// always belongs to the profiler.
		/// </summary>
		private const InstrumentationFlag AdoNetProfiler = InstrumentationFlag.SqlClient | InstrumentationFlag.AdoNet;

		/// <summary>
		/// For the SqlClient listeners: both Entity Framework and the profiler trace the very same command.
		/// </summary>
		internal static bool IsCommandTracedByOtherModule(IApmAgent agent, IDbCommand command = null) =>
			IsCommandTracedBy(agent, EntityFramework | AdoNetProfiler, command);

		/// <summary>
		/// For the Entity Framework modules: the profiler's ADO.NET integrations trace the very same command.
		/// </summary>
		internal static bool IsCommandTracedByAdoNetModule(IApmAgent agent, IDbCommand command = null) =>
			IsCommandTracedBy(agent, AdoNetProfiler, command);

		private static bool IsCommandTracedBy(IApmAgent agent, InstrumentationFlag modules, IDbCommand command)
		{
			if (agent?.Tracer.CurrentSpan is not Span span || (span.InstrumentationFlag & modules) == 0)
				return false;

			// Where the command is available, require the competing span to describe this very command, as the profiler
			// itself does in DbSpanFactory. A competing span for a different command must not suppress this one.
			return command is null || span.Name == DbSpanCommon.GetDbSpanName(command);
		}
	}
}
