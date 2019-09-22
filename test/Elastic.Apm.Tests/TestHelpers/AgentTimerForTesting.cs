using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions.Extensions;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal interface IAgentTimerForTesting : IAgentTimer
	{
		IReadOnlyList<Task> PendingDelayTasks { get; }

		int PendingDelayTasksCount { get; }

		void WaitForTimeToPassAndUntil(TimeSpan timeSpan, Func<bool> untilCondition = null, Func<string> dbgDesc = null);
	}

	internal static class AgentTimerForTestsExtensions
	{
		internal static void WaitForTimeToPass(this IAgentTimerForTesting thisObj, TimeSpan timeSpan, Func<string> dbgDesc = null) =>
			thisObj.WaitForTimeToPassAndUntil(timeSpan, /* untilCondition: */ null, dbgDesc);
	}

	internal class AgentTimerForTesting : IAgentTimerForTesting
	{
		private const string ThisClassName = nameof(AgentTimerForTesting);
		private readonly AgentTimer _realAgentTimer = new AgentTimer();

		private readonly IApmLogger _logger;
		private readonly MockAgentTimer _mockAgentTimer = new MockAgentTimer("AgentTimerForTesting internal");

		internal AgentTimerForTesting(IApmLogger logger = null) =>
			_logger = logger == null ? (IApmLogger)new NoopLogger() : logger.Scoped(ThisClassName);

		public AgentTimeInstant Now => _realAgentTimer.Now;

		public IReadOnlyList<Task> PendingDelayTasks => _mockAgentTimer.PendingDelayTasks;

		public int PendingDelayTasksCount => _mockAgentTimer.PendingDelayTasksCount;

		public async Task Delay(AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			var realNow = _realAgentTimer.Now;
			CatchUpMockTimerToReal(realNow);
			await Task.WhenAll(_realAgentTimer.Delay(until, cancellationToken)
				, _mockAgentTimer.Delay(_mockAgentTimer.Now + (until - realNow), cancellationToken));
		}

		private void CatchUpMockTimerToReal(AgentTimeInstant realNow)
		{
			var realSpanSinceStarted = realNow - _realAgentTimer.WhenStarted;
			var mockSpanSinceStarted = _mockAgentTimer.Now - _mockAgentTimer.WhenStarted;
			Assertion.IfEnabled?.That(mockSpanSinceStarted <= realSpanSinceStarted
				, "Real timer should not be behind the mock one."
				+ $" {nameof(mockSpanSinceStarted)}: {mockSpanSinceStarted}. {nameof(realSpanSinceStarted)}: {realSpanSinceStarted}.");
			_mockAgentTimer.FastForward(realSpanSinceStarted - mockSpanSinceStarted);
		}

		public void WaitForTimeToPassAndUntil(TimeSpan timeSpan, Func<bool> untilCondition = null, Func<string> dbgDesc = null)
		{
			var realNow = _realAgentTimer.Now;
			var targetInstant = realNow + timeSpan;

			while (true)
			{
				CatchUpMockTimerToReal(realNow);
				var leftToWait = targetInstant - realNow;
				if (leftToWait <= TimeSpan.Zero) break;

				_logger.Debug()
					?.Log("Waiting for time to pass..."
						+ $" dbgDesc: {(dbgDesc?.Invoke()).AsNullableToString()}. leftToWait: {leftToWait}. realNow: {realNow}."
						+ $" timeSpan: {timeSpan}. targetInstant: {targetInstant}");

				Thread.Sleep(leftToWait);
				_mockAgentTimer.FastForward(leftToWait);
				realNow = _realAgentTimer.Now;
			}

			if (untilCondition != null) WaitUntil(untilCondition, dbgDesc);
		}

		private void WaitUntil(Func<bool> untilCondition, Func<string> dbgDesc)
		{
			_logger.Debug()?.Log($"Waiting until condition is true... dbgDesc: {(dbgDesc?.Invoke()).AsNullableToString()}.");

			var maxTotalTimeToWait = 30.Seconds();
			var timeToWaitBetweenChecks = 10.Milliseconds();
			var minTimeBetweenLogs = 1.Second();

			var stopwatch = Stopwatch.StartNew();
			var attemptCount = 0;
			TimeSpan? elapsedOnLastWaitingLog = null;
			while (true)
			{
				++attemptCount;
				if (untilCondition())
				{
					_logger.Info()?.Log($"Wait-until-condition is true. attemptCount: {attemptCount}", attemptCount);
					return;
				}

				var elapsedTime = stopwatch.Elapsed;
				if (elapsedTime > maxTotalTimeToWait)
				{
					throw new XunitException("Wait-until-condition is still false even after max allotted time to wait."
						+ $" dbgDesc: {(dbgDesc?.Invoke()).AsNullableToString()}."
						+ $" elapsedTime: {elapsedTime.ToHmsInSeconds()}."
						+ $" attemptCount: {attemptCount}.");
				}

				if (!elapsedOnLastWaitingLog.HasValue || elapsedOnLastWaitingLog.Value + minTimeBetweenLogs <= elapsedTime)
				{
					_logger.Debug()
						?.Log("Delaying until next check..."
							+ $" dbgDesc: {(dbgDesc?.Invoke()).AsNullableToString()}."
							+ $" elapsedTime: {elapsedTime.ToHms()}."
							+ $" attemptCount: {attemptCount}."
							+ $" maxTotalTimeToWait: {maxTotalTimeToWait}."
							+ $" timeToWaitBetweenChecks: {timeToWaitBetweenChecks}.");
					elapsedOnLastWaitingLog = elapsedTime;
				}

				Thread.Sleep(timeToWaitBetweenChecks);
			}
		}
	}
}
