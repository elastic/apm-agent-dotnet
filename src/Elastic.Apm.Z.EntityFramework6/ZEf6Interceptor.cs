using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Runtime.CompilerServices;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Z.EntityFramework6
{
	/// <summary>
	/// An interceptor that automatically creates spans for DB operations executed by Entity Framework 6 (EF6) on behalf of the
	/// application.
	/// See
	/// <see href="https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup.html#entity-framework-6">
	/// .NET Agent documentation
	/// on how to set up auto instrumentation for Entity Framework 6
	/// </see>
	/// </summary>
	public class ZEf6Interceptor : IDbCommandInterceptor
	{
		private const string ThisClassName = nameof(ZEf6Interceptor);

		private readonly Lazy<Impl> _impl = new Lazy<Impl>(() => new Impl());

		public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptCtx) =>
			CreateImplIfReady()?.StartSpan(command, interceptCtx);

		public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptCtx) =>
			CreateImplIfReady()?.EndSpan(command, interceptCtx);

		public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptCtx) =>
			CreateImplIfReady()?.StartSpan(command, interceptCtx);

		public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptCtx) =>
			CreateImplIfReady()?.EndSpan(command, interceptCtx);

		public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptCtx) =>
			CreateImplIfReady()?.StartSpan(command, interceptCtx);

		public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptCtx) =>
			CreateImplIfReady()?.EndSpan(command, interceptCtx);

		/// <summary>
		/// DB spans can be created only when there's a current transaction
		/// which in turn means agent singleton instance should already be created.
		/// </summary>
		private Impl CreateImplIfReady() => Agent.IsConfigured ? _impl.Value : null;

		private class Impl
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private const string ThisClassName = ZEf6Interceptor.ThisClassName + "." + nameof(Impl);

			private readonly IApmLogger _logger;
			private readonly string _userStateKey;

			internal Impl()
			{
				var thisInstanceDbgName = ThisClassName + "#" + RuntimeHelpers.GetHashCode(this).ToString("X");
				_logger = Agent.Instance.Logger.Scoped(thisInstanceDbgName);
				_userStateKey = "Elastic.Apm.EntityFramework6." + thisInstanceDbgName;
			}

			private void LogEvent(string message, IDbCommand command, DbInterceptionContext interceptCtx, string dbgOriginalCaller) =>
				_logger.Trace()
					?.Log(message
						+ " DbCommandInterceptionContext: #{ObjectInstanceHashCode}"
						+ " Caller: {Caller}."
						+ " Statement: {DbStatement} "
						+ " Is async: {IsAsync}"
						, RuntimeHelpers.GetHashCode(interceptCtx).ToString("X")
						, dbgOriginalCaller.AsNullableToString()
						, (command?.CommandText).AsNullableToString()
						, interceptCtx.IsAsync
					);

			internal void StartSpan<TResult>(IDbCommand command, DbCommandInterceptionContext<TResult> interceptCtx
				, [CallerMemberName] string dbgCaller = null
			)
			{
				try
				{
					DoStartSpan(command, interceptCtx, dbgCaller);
				}
				catch (Exception ex)
				{
					_logger.Error()?.LogException(ex, "Processing of DB-operation-started event failed");
				}
			}

			internal void EndSpan<TResult>(IDbCommand command, DbCommandInterceptionContext<TResult> interceptCtx
				, [CallerMemberName] string dbgCaller = null
			)
			{
				try
				{
					DoEndSpan(command, interceptCtx, dbgCaller);
				}
				catch (Exception ex)
				{
					_logger.Error()?.LogException(ex, "Processing of DB-operation-ended event failed");
				}
			}

			private void DoStartSpan<TResult>(IDbCommand command, DbCommandInterceptionContext<TResult> interceptCtx, string dbgOriginalCaller)
			{
				if (Agent.Instance.Tracer.CurrentTransaction == null)
				{
					_logger.Debug()?.Log("There's' no current transaction - skipping starting span for DB-operation-started event");
					return;
				}

				LogEvent("DB operation started - starting a new span...", command, interceptCtx, dbgOriginalCaller);

				var span = DbSpanCommon.StartSpan(Agent.Instance, command);
				interceptCtx.SetUserState(_userStateKey, span);
			}

			private void DoEndSpan<TResult>(IDbCommand command, DbCommandInterceptionContext<TResult> interceptCtx, string dbgOriginalCaller)
			{
				var span = (Span)interceptCtx.FindUserState(_userStateKey);
				if (span == null)
				{
					_logger.Debug()
						?.Log("Span is not found in DbCommandInterceptionContext's UserState"
							+ " - skipping ending the corresponding span for DB-operation-ended event");
					return;
				}

				LogEvent("DB operation ended - ending the corresponding span...", command, interceptCtx, dbgOriginalCaller);

				DbSpanCommon.EndSpan(span, command);
			}
		}
	}
}
