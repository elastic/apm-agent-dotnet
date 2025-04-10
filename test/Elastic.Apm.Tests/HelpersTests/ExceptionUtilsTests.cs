// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class ExceptionUtilsTests
	{
		public static TheoryData<string, Action> DoSwallowingExceptionsVariantsToTest => new TheoryData<string, Action>
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
			var startMsg = string.Format(ExceptionUtils.MethodStartingMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.CurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(startMsg));
			var exitMsg = string.Format(exitMsgFmt.Replace("{MethodName}", "{0}"), DbgUtils.CurrentMethodName());
			mockLogger.Lines.Should().Contain(line => line.Contains(exitMsg));
		}
	}
}
