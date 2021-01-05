// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class ExceptionUtils
	{
		internal const string MethodExitingCancelledMsgFmt = "{MethodName} is about to exit because it was cancelled, which is expected on shutdown";
		internal const string MethodExitingExceptionMsgFmt = "{MethodName} is about to exit because of exception";
		internal const string MethodExitingNormallyMsgFmt = "{MethodName} is about to exit normally";

		internal const string MethodStartingMsgFmt = "{MethodName} entered";
		private const string ThisClassName = nameof(ExceptionUtils);

		internal static void DoSwallowingExceptions(IApmLogger loggerArg, Action action, bool shouldSwallowCancellation = true
			, [CallerMemberName] string dbgCallerMethodName = null
		)
		{
			var logger = loggerArg.Scoped($"{ThisClassName}.{nameof(DoSwallowingExceptions)}");
			try
			{
				logger.Debug()?.Log(MethodStartingMsgFmt, dbgCallerMethodName);
				action();
				logger.Debug()?.Log(MethodExitingNormallyMsgFmt, dbgCallerMethodName);
			}
			catch (OperationCanceledException ex)
			{
				logger.Debug()?.LogException(ex, MethodExitingCancelledMsgFmt, dbgCallerMethodName);

				if (!shouldSwallowCancellation) throw;
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, MethodExitingExceptionMsgFmt, dbgCallerMethodName);
			}
		}

		internal static async Task DoSwallowingExceptions(IApmLogger loggerArg, Func<Task> asyncAction, bool shouldSwallowCancellation = true
			, [CallerMemberName] string dbgCallerMethodName = null
		)
		{
			var logger = loggerArg.Scoped($"{ThisClassName}.{nameof(DoSwallowingExceptions)}");
			try
			{
				logger.Debug()?.Log(MethodStartingMsgFmt + ". Current thread: {ThreadDesc}", dbgCallerMethodName, DbgUtils.CurrentThreadDesc);
				await asyncAction().ConfigureAwait(false);
				logger.Debug()
					?.Log(MethodExitingNormallyMsgFmt + ". Current thread: {ThreadDesc}", dbgCallerMethodName
						, DbgUtils.CurrentThreadDesc);
			}
			catch (OperationCanceledException ex)
			{
				logger.Debug()
					?.LogException(ex, MethodExitingCancelledMsgFmt + ". Current thread: {ThreadDesc}", dbgCallerMethodName
						, DbgUtils.CurrentThreadDesc);

				if (!shouldSwallowCancellation) throw;
			}
			catch (Exception ex)
			{
				logger.Error()
					?.LogException(ex, MethodExitingExceptionMsgFmt + ". Current thread: {ThreadDesc}", dbgCallerMethodName
						, DbgUtils.CurrentThreadDesc);
			}
		}

		internal static void DoSwallowingExceptionsExceptCancellation(IApmLogger logger, Action action
			, [CallerMemberName] string dbgCallerMethodName = null
		) =>
			DoSwallowingExceptions(logger, action, /* shouldSwallowCancellation */ false, dbgCallerMethodName);

		internal static Task DoSwallowingExceptionsExceptCancellation(IApmLogger logger, Func<Task> asyncAction
			, [CallerMemberName] string dbgCallerMethodName = null
		) =>
			DoSwallowingExceptions(logger, asyncAction, /* shouldSwallowCancellation */ false, dbgCallerMethodName);
	}
}
