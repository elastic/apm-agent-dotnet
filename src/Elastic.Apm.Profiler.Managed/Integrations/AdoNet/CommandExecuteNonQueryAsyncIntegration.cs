// <copyright file="CommandExecuteNonQueryAsyncIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Data;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;
using static Elastic.Apm.Profiler.Managed.Integrations.AdoNet.AdoNetTypeNames;

namespace Elastic.Apm.Profiler.Managed.Integrations.AdoNet
{
    /// <summary>
    /// CallTarget instrumentation for:
    /// Task[int] [Command].ExecuteNonQueryAsync(CancellationToken)
    /// </summary>
	[InstrumentMySqlAttribute(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentNpgsql(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentOracleManagedDataAccess(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentOracleManagedDataAccessCore(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentSqlite(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSql(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSqlClient(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentMicrosoftDataSqlClient(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentSystemData(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataCommon(Method = ExecuteNonQueryAsync, ReturnType = TaskInt32, ParameterTypes = new [] { ClrTypeNames.CancellationToken })]
    public class CommandExecuteNonQueryAsyncIntegration
	{
		/// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, CancellationToken cancellationToken)
		{
			var command = (IDbCommand)instance;
			return new CallTargetState(DbSpanFactory<TTarget>.CreateSpan(Agent.Instance, command), command);
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value, in an async scenario will be T of Task of T</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
			DbSpanFactory<TTarget>.EndSpan(Agent.Instance, (IDbCommand)instance, (ISpan)state.Segment, exception);
			return returnValue;
        }
    }
}
