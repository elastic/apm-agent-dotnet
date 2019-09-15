using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class ExceptionUtilsTests
	{
		public static TheoryData DoSwallowingExceptionsVariantsToTest => new TheoryData<string, Action>
		{
			{ ExceptionUtils.MethodExitingNormallyMsgFmt, () => { } },
			{ ExceptionUtils.MethodExitingCancelledMsgFmt, () => new CancellationToken(true).ThrowIfCancellationRequested() },
			{ ExceptionUtils.MethodExitingExceptionMsgFmt, () => throw new DummyTestException() }
		};

		[Theory]
		[MemberData(nameof(DoSwallowingExceptionsVariantsToTest))]
		public void DoSwallowingExceptions_test(string exitMsgFmt, Action action)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var sideEffect = 0;
			ExceptionUtils.DoSwallowingExceptions(mockLogger, () =>
			{
				++sideEffect;
				action();
			});
			sideEffect.Should().Be(1);
			var startMsg = string.Format(ExceptionUtils.MethodStartingMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(startMsg));
			var exitMsg = string.Format(exitMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(exitMsg));
		}

		[Theory]
		[MemberData(nameof(DoSwallowingExceptionsVariantsToTest))]
		public async Task DoSwallowingExceptions_async_test(string exitMsgFmt, Action action)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var sideEffect = 0;
			await ExceptionUtils.DoSwallowingExceptions(mockLogger, async () =>
			{
				await Task.Yield();
				++sideEffect;
				action();
			});
			sideEffect.Should().Be(1);
			var startMsg = ExceptionUtils.MethodStartingMsgFmt.Replace("{MethodName}", DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(startMsg));
			var exitMsg = exitMsgFmt.Replace("{MethodName}", DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(exitMsg));
		}

		[Theory]
		[MemberData(nameof(DoSwallowingExceptionsVariantsToTest))]
		public void DoSwallowingExceptionsExceptCancellation_test(string exitMsgFmt, Action action)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var sideEffect = 0;

			Action actionToTest = () => ExceptionUtils.DoSwallowingExceptionsExceptCancellation(mockLogger, () =>
			{
				++sideEffect;
				action();
			});

			if (exitMsgFmt == ExceptionUtils.MethodExitingCancelledMsgFmt)
				actionToTest.Should().Throw<OperationCanceledException>();
			else
				actionToTest();

			sideEffect.Should().Be(1);
			var startMsg = string.Format(ExceptionUtils.MethodStartingMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(startMsg));
			var exitMsg = string.Format(exitMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(exitMsg));
		}

		[Theory]
		[MemberData(nameof(DoSwallowingExceptionsVariantsToTest))]
		public async Task DoSwallowingExceptionsExceptCancellation_async_test(string exitMsgFmt, Action action)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var sideEffect = 0;

			Func<Task> asyncActionToTest = () => ExceptionUtils.DoSwallowingExceptionsExceptCancellation(mockLogger, async () =>
			{
				await Task.Yield();
				++sideEffect;
				action();
			});

			if (exitMsgFmt == ExceptionUtils.MethodExitingCancelledMsgFmt)
				await asyncActionToTest.Should().ThrowAsync<OperationCanceledException>();
			else
				await asyncActionToTest();

			sideEffect.Should().Be(1);
			var startMsg = string.Format(ExceptionUtils.MethodStartingMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(startMsg));
			var exitMsg = string.Format(exitMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.GetCurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(exitMsg));
		}
	}
}
