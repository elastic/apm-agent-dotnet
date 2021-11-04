// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="CommandExecuteReaderWithBehaviorAsyncIntegration.cs" company="Datadog">
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
    /// Task[*DataReader] [Command].ExecuteReaderAsync(CommandBehavior, CancellationToken)
    /// Task[DbDataReader] [Command].ExecuteDbDataReaderAsync(CommandBehavior, CancellationToken)
    /// </summary>
	[InstrumentMySqlAttribute(Method = ExecuteReaderAsync, ReturnType = MySql.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentNpgsql(Method = ExecuteReaderAsync, ReturnType = Npgsql.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentOracleManagedDataAccess(Method = ExecuteReaderAsync, ReturnType = OracleManagedDataAccess.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentOracleManagedDataAccessCore(Method = ExecuteReaderAsync, ReturnType = OracleManagedDataAccess.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentMicrosoftDataSqlite(Method = ExecuteReaderAsync, ReturnType = MicrosoftDataSqlite.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSqlite(Method = ExecuteReaderAsync, ReturnType = SystemDataSqlite.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSql(Method = ExecuteReaderAsync, ReturnType = SystemDataSqlServer.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSqlClient(Method = ExecuteReaderAsync, ReturnType = SystemDataSqlServer.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentMicrosoftDataSqlClient(Method = ExecuteReaderAsync, ReturnType = MicrosoftDataSqlServer.TaskDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]

	[InstrumentMySqlAttribute(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentNpgsql(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentOracleManagedDataAccess(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentOracleManagedDataAccessCore(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentMicrosoftDataSqlite(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSql(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataSqlClient(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentMicrosoftDataSqlClient(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]

	[InstrumentSystemData(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	[InstrumentSystemDataCommon(Method = ExecuteDbDataReaderAsync, ReturnType = TaskDbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior, ClrTypeNames.CancellationToken })]
	public class CommandExecuteReaderWithBehaviorAsyncIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TBehavior">Command Behavior type</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="commandBehavior">Command behavior</param>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TBehavior>(TTarget instance, TBehavior commandBehavior, CancellationToken cancellationToken)
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
