// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using FluentAssertions.Extensions;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.Utilities
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

		private readonly IApmLogger _logger;
		private readonly MockAgentTimer _mockAgentTimer = new MockAgentTimer("AgentTimerForTesting internal");
		private readonly AgentTimer _realAgentTimer = new AgentTimer();

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
						+ $" dbgDesc: {ObjectExtensions.AsNullableToString((dbgDesc?.Invoke()))}. leftToWait: {leftToWait}. realNow: {realNow}."
						+ $" timeSpan: {timeSpan}. targetInstant: {targetInstant}");

				Thread.Sleep(leftToWait);
				_mockAgentTimer.FastForward(leftToWait);
				realNow = _realAgentTimer.Now;
			}

			if (untilCondition != null) WaitUntil(untilCondition, dbgDesc);
		}

		private void WaitUntil(Func<bool> untilCondition, Func<string> dbgDesc)
		{
			_logger.Debug()?.Log($"Waiting until condition is true... dbgDesc: {ObjectExtensions.AsNullableToString((dbgDesc?.Invoke()))}.");

			var maxTotalTimeToWait = 5.Minutes();
			var timeToWaitBetweenChecks = 10.Milliseconds();
			var minTimeBetweenLogs = 1.Seconds();

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
						+ $" dbgDesc: {ObjectExtensions.AsNullableToString((dbgDesc?.Invoke()))}."
						+ $" elapsedTime: {TimeExtensions.ToHmsInSeconds(elapsedTime)}."
						+ $" attemptCount: {attemptCount}.");
				}

				if (!elapsedOnLastWaitingLog.HasValue || elapsedOnLastWaitingLog.Value + minTimeBetweenLogs <= elapsedTime)
				{
					_logger.Debug()
						?.Log("Delaying until next check..."
							+ $" dbgDesc: {ObjectExtensions.AsNullableToString((dbgDesc?.Invoke()))}."
							+ $" elapsedTime: {TimeExtensions.ToHms(elapsedTime)}."
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
