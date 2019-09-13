using System;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class DisposableHelperTests
	{
		[Fact]
		public void dispose_executes_only_on_the_first_call()
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var sideEffect = false;

			void DisposeAction()
			{
				sideEffect = true;
				// ReSharper disable once AccessToModifiedClosure
				mockLogger.Debug()?.Log($"Inside {nameof(DisposeAction)}");
			}

			var disposableHelper = new DisposableHelper();
			const string disposableDesc = "test_disposable";
			var isDisposedByThisCall = disposableHelper.DoOnce(mockLogger, disposableDesc, DisposeAction);
			isDisposedByThisCall.Should().BeTrue();
			sideEffect.Should().BeTrue();
			disposableHelper.HasStarted.Should().BeTrue();
			mockLogger.Lines.Should().HaveCount(3);
			mockLogger.Lines.First().Should().MatchEquivalentOf($"*Starting to dispose {disposableDesc}*");
			mockLogger.Lines.Skip(1).First().Should().MatchEquivalentOf($"*Inside {nameof(DisposeAction)}*");
			mockLogger.Lines.Skip(2).First().Should().MatchEquivalentOf($"*Finished disposing {disposableDesc}*");

			mockLogger = new TestLogger(LogLevel.Trace);
			sideEffect = false;
			isDisposedByThisCall = disposableHelper.DoOnce(mockLogger, disposableDesc, DisposeAction);
			isDisposedByThisCall.Should().BeFalse();
			sideEffect.Should().BeFalse();
			disposableHelper.HasStarted.Should().BeTrue();
			mockLogger.Lines.Should().ContainSingle();
			mockLogger.Lines.First().Should().MatchEquivalentOf($"*{disposableDesc}*already disposed*");
		}

		[Fact]
		internal void calling_Dispose_again_while_previous_call_is_still_in_progress()
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			using (new DummyClassReenteringDispose(mockLogger)) { }
			mockLogger.Lines.Where(line => line.Contains("Critical")
					&& line.Contains(string.Format(DisposableHelper.AnotherCallStillInProgressMsg, nameof(DummyClassReenteringDispose))))
				.Should()
				.ContainSingle();
		}

		private class DummyClassReenteringDispose : IDisposable
		{
			private readonly DisposableHelper _disposableHelper = new DisposableHelper();
			private readonly IApmLogger _logger;

			internal DummyClassReenteringDispose(IApmLogger logger) => _logger = logger;

			public void Dispose() => _disposableHelper.DoOnce(_logger, nameof(DummyClassReenteringDispose), Dispose);
		}
	}
}
