// <copyright file="CommandExecuteReaderWithBehaviorIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Profiler.Managed.CallTarget;
using static Elastic.Apm.Profiler.Managed.Integrations.AdoNet.AdoNetTypeNames;

namespace Elastic.Apm.Profiler.Managed.Integrations.AdoNet
{
    /// <summary>
    /// CallTarget instrumentation for:
    /// [*]DataReader [Command].ExecuteReader(CommandBehavior)
    /// [*]DataReader [Command].ExecuteDbDataReader(CommandBehavior)
    /// </summary>
	[InstrumentMySqlAttribute(Method = ExecuteReader, ReturnType = MySql.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentNpgsql(Method = ExecuteReader, ReturnType = Npgsql.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentOracleManagedDataAccess(Method = ExecuteReader, ReturnType = OracleManagedDataAccess.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentOracleManagedDataAccessCore(Method = ExecuteReader, ReturnType = OracleManagedDataAccess.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentMicrosoftDataSqlite(Method = ExecuteReader, ReturnType = MicrosoftDataSqlite.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentSystemDataSqlite(Method = ExecuteReader, ReturnType = SystemDataSqlite.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentSystemDataSql(Method = ExecuteReader, ReturnType = SystemDataSqlServer.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentSystemDataSqlClient(Method = ExecuteReader, ReturnType = SystemDataSqlServer.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentMicrosoftDataSqlClient(Method = ExecuteReader, ReturnType = MicrosoftDataSqlServer.DataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]

	[InstrumentMySqlAttribute(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentNpgsql(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentOracleManagedDataAccess(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentOracleManagedDataAccessCore(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentMicrosoftDataSqlite(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentSystemDataSql(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentSystemDataSqlClient(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	[InstrumentMicrosoftDataSqlClient(Method = ExecuteDbDataReader, ReturnType = DbDataReader, ParameterTypes = new [] { AdoNetTypeNames.CommandBehavior })]
	public class CommandExecuteReaderWithBehaviorIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TBehavior">Command Behavior type</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="commandBehavior">Command behavior</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TBehavior>(TTarget instance, TBehavior commandBehavior)
        {
			var command = (IDbCommand)instance;
			return new CallTargetState(DbSpanFactory<TTarget>.CreateSpan(Agent.Instance, command), command);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Task of HttpResponse message instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
			DbSpanFactory<TTarget>.EndSpan(Agent.Instance, (IDbCommand)instance, (ISpan)state.Segment, exception);
			return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
