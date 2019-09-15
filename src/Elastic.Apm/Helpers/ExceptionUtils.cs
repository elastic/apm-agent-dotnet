using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class ExceptionUtils
	{
		internal const string MethodStartingMsgFmt = "{MethodName} starting...";
		internal const string MethodExitingNormallyMsgFmt = "{MethodName} is about to exit normally";
		internal const string MethodExitingCancelledMsgFmt = "{MethodName} is about to exit because it was cancelled, which is expected on shutdown";
		internal const string MethodExitingExceptionMsgFmt = "{MethodName} is about to exit because of exception";

		internal static void DoSwallowingExceptions(IApmLogger logger, Action action, bool shouldSwallowCancellation = true
			, [CallerMemberName] string dbgCallerMethodName = null)
		{
			try
			{
				logger.Debug()?.Log(MethodStartingMsgFmt, dbgCallerMethodName);
				action();
				logger.Debug()?.Log(MethodExitingNormallyMsgFmt, dbgCallerMethodName);
			}
			catch (OperationCanceledException ex)
			{
				logger.Debug()?.LogException(ex, MethodExitingCancelledMsgFmt, dbgCallerMethodName);

				if (! shouldSwallowCancellation) throw;
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, MethodExitingExceptionMsgFmt, dbgCallerMethodName);
			}
		}

		internal static async Task DoSwallowingExceptions(IApmLogger logger, Func<Task> asyncAction, bool shouldSwallowCancellation = true
			, [CallerMemberName] string dbgCallerMethodName = null)
		{
			try
			{
				logger.Debug()?.Log(MethodStartingMsgFmt, dbgCallerMethodName);
				await asyncAction();
				logger.Debug()?.Log(MethodExitingNormallyMsgFmt, dbgCallerMethodName);
			}
			catch (OperationCanceledException ex)
			{
				logger.Debug()?.LogException(ex, MethodExitingCancelledMsgFmt, dbgCallerMethodName);

				if (! shouldSwallowCancellation) throw;
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, MethodExitingExceptionMsgFmt, dbgCallerMethodName);
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
