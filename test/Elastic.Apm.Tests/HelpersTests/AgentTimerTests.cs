// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class AgentTimerTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(AgentTimerTests);

		private static readonly TimeSpan ShortTimeAfterTaskStarted = 10.Milliseconds();
		private static readonly TimeSpan VeryLongTimeout = 1.Days();
		private readonly IApmLogger _logger;

		public AgentTimerTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) => _logger = LoggerBase.Scoped(ThisClassName);

		internal interface ISutEnv
		{
			IAgentTimerForTesting AgentTimer { get; }

			Task AwaitOrTimeoutCall(TimeSpan timeout, out Task delayTask, CancellationToken cancellationToken = default);

			void CancelTask();

			void CompleteTaskSuccessfully();

			void FaultTask();

			Task TryAwaitOrTimeoutCall(TimeSpan timeout, out Task delayTask, CancellationToken cancellationToken = default);

			void VerifyAwaitCompletedSuccessfully(Task awaitOrTimeoutTask, Task delayTask);

			void VerifyAwaitTimeout(Task tryAwaitOrTimeoutTask, Task delayTask);

			void VerifyCancelled(Task xyzAwaitOrTimeoutTask, Task delayTask);

			void VerifyDelayCancelled(Task awaitOrTimeoutTask, Task delayTask);

			void VerifyFaulted(Task xyzAwaitOrTimeoutTask, Task delayTask);

			void VerifyTryAwaitCompletedSuccessfully(Task tryAwaitOrTimeoutTask, Task delayTask);

			void VerifyTryAwaitTimeout(Task tryAwaitOrTimeoutTask, Task delayTask);
		}

		public static IEnumerable<object[]> AwaitOrTimeoutVariantsToTest =>
			AwaitOrTimeoutVariantsToTestImpl().Select(t => new object[] { t.Item1, t.Item2 });

		internal static IEnumerable<ValueTuple<string, Func<IAgentTimerForTesting, ISutEnv>>> AwaitOrTimeoutTaskVariantsToTest()
		{
			yield return ("Task", agentTimer => new SutEnv<object>(agentTimer));
			yield return ("Task<int>, 123", agentTimer => new SutEnv<int>(agentTimer, 123));
			yield return ("Task<string>, `456'", agentTimer => new SutEnv<string>(agentTimer, "456"));
		}

		private static IEnumerable<ValueTuple<string, Func<IApmLogger, ISutEnv>>> AwaitOrTimeoutVariantsToTestImpl()
		{
			var agentTimerVariants = new ValueTuple<string, Func<IApmLogger, IAgentTimerForTesting>>[]
			{
				("Real timer", logger => new AgentTimerForTesting(logger)), ("Mock timer", logger => new MockAgentTimer(logger: logger))
			};

			var counter = 0;
			foreach (var taskVariant in AwaitOrTimeoutTaskVariantsToTest())
			{
				foreach (var agentTimerVariant in agentTimerVariants)
				{
					yield return (++counter + ": " + taskVariant.Item1 + " / " + agentTimerVariant.Item1
						, logger => taskVariant.Item2(agentTimerVariant.Item2(logger)));
				}
			}
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void TryAwaitOrTimeout_task_completed_successfully_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var tryAwaitOrTimeoutTask = sutEnv.TryAwaitOrTimeoutCall(VeryLongTimeout, out var delayTask);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted, () => $"dbgVariantDesc: {dbgVariantDesc}. delayTask: {delayTask.Status}");
			tryAwaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			sutEnv.CompleteTaskSuccessfully();

			sutEnv.VerifyTryAwaitCompletedSuccessfully(tryAwaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void TryAwaitOrTimeout_task_cancelled_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var tryAwaitOrTimeoutTask = sutEnv.TryAwaitOrTimeoutCall(VeryLongTimeout, out var delayTask);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. tryAwaitOrTimeoutTask: {tryAwaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			tryAwaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			sutEnv.CancelTask();

			sutEnv.VerifyCancelled(tryAwaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void TryAwaitOrTimeout_task_faulted_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var tryAwaitOrTimeoutTask = sutEnv.TryAwaitOrTimeoutCall(VeryLongTimeout, out var delayTask);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. tryAwaitOrTimeoutTask: {tryAwaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			tryAwaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			sutEnv.FaultTask();

			sutEnv.VerifyFaulted(tryAwaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void TryAwaitOrTimeout_Delay_cancelled_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var cts = new CancellationTokenSource();
			var tryAwaitOrTimeoutTask = sutEnv.TryAwaitOrTimeoutCall(VeryLongTimeout, out var delayTask, cts.Token);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. tryAwaitOrTimeoutTask: {tryAwaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			tryAwaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			cts.Cancel();
			WaitForTaskCancelled(tryAwaitOrTimeoutTask);

			sutEnv.VerifyDelayCancelled(tryAwaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void AwaitOrTimeout_task_completed_successfully_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var awaitOrTimeoutTask = sutEnv.AwaitOrTimeoutCall(VeryLongTimeout, out var delayTask);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. awaitOrTimeoutTask: {awaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			awaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			sutEnv.CompleteTaskSuccessfully();

			sutEnv.VerifyAwaitCompletedSuccessfully(awaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void AwaitOrTimeout_task_cancelled_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var awaitOrTimeoutTask = sutEnv.AwaitOrTimeoutCall(5.Seconds(), out var delayTask);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. awaitOrTimeoutTask: {awaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			awaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			sutEnv.CancelTask();

			sutEnv.VerifyCancelled(awaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void AwaitOrTimeout_task_faulted_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var awaitOrTimeoutTask = sutEnv.AwaitOrTimeoutCall(VeryLongTimeout, out var delayTask);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. awaitOrTimeoutTask: {awaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			awaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			sutEnv.FaultTask();

			sutEnv.VerifyFaulted(awaitOrTimeoutTask, delayTask);
		}

		[Theory]
		[MemberData(nameof(AwaitOrTimeoutVariantsToTest))]
		internal void AwaitOrTimeout_Delay_cancelled_test(string dbgVariantDesc, Func<IApmLogger, ISutEnv> sutEnvCreator)
		{
			var sutEnv = sutEnvCreator(_logger);
			var cts = new CancellationTokenSource();
			var awaitOrTimeoutTask = sutEnv.AwaitOrTimeoutCall(VeryLongTimeout, out var delayTask, cts.Token);

			sutEnv.AgentTimer.WaitForTimeToPass(ShortTimeAfterTaskStarted
				, () => $"dbgVariantDesc: {dbgVariantDesc}. awaitOrTimeoutTask: {awaitOrTimeoutTask.Status}. delayTask: {delayTask.Status}");
			awaitOrTimeoutTask.IsCompleted.Should().BeFalse();

			cts.Cancel();
			WaitForTaskCancelled(awaitOrTimeoutTask);

			sutEnv.VerifyDelayCancelled(awaitOrTimeoutTask, delayTask);
		}

		private static void WaitForTaskCancelled(Task task)
		{
			try
			{
				if (!task.IsCanceled)
					Task.WaitAll(new [] { task });
			}
			catch
			{
				// Might throw if task goes into cancelled state just as we call "WaitAll".
			}
		}

		private class SutEnv<TResult> : ISutEnv
		{
			private readonly CancellationToken _cancellationToken = new CancellationToken(true);
			private readonly DummyTestException _dummyTestException = new DummyTestException();
			private readonly bool _isVoid;
			private readonly TResult _resultValue;
			private readonly TaskCompletionSource<TResult> _taskToAwaitTcs = new TaskCompletionSource<TResult>();

			internal SutEnv(IAgentTimerForTesting agentTimer)
			{
				_isVoid = true;
				_resultValue = default;
				AgentTimer = agentTimer;
			}

			internal SutEnv(IAgentTimerForTesting agentTimer, TResult resultValue)
			{
				_isVoid = false;
				resultValue.Should().NotBe(default);
				_resultValue = resultValue;
				AgentTimer = agentTimer;
			}

			public IAgentTimerForTesting AgentTimer { get; }

			public Task TryAwaitOrTimeoutCall(TimeSpan timeout, out Task delayTask, CancellationToken cancellationToken = default)
			{
				AgentTimer.PendingDelayTasksCount.Should().Be(0);
				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();

				var result = _isVoid
					? (Task)AgentTimer.TryAwaitOrTimeout((Task)_taskToAwaitTcs.Task, AgentTimer.Now + timeout, cancellationToken)
					: AgentTimer.TryAwaitOrTimeout(_taskToAwaitTcs.Task, AgentTimer.Now + timeout, cancellationToken);

				result.IsCompleted.Should().BeFalse();
				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();
				AgentTimer.PendingDelayTasksCount.Should().Be(1);
				delayTask = AgentTimer.PendingDelayTasks.First();
				delayTask.IsCompleted.Should().BeFalse();

				return result;
			}

			public Task AwaitOrTimeoutCall(TimeSpan timeout, out Task delayTask, CancellationToken cancellationToken = default)
			{
				var result = _isVoid
					? AgentTimer.AwaitOrTimeout((Task)_taskToAwaitTcs.Task, AgentTimer.Now + timeout, cancellationToken)
					: AgentTimer.AwaitOrTimeout(_taskToAwaitTcs.Task, AgentTimer.Now + timeout, cancellationToken);

				AgentTimer.PendingDelayTasksCount.Should().Be(1);
				delayTask = AgentTimer.PendingDelayTasks.First();
				delayTask.IsCompleted.Should().BeFalse();

				return result;
			}

			private void UnpackTryAwaitOrTimeoutTaskResult(Task tryAwaitOrTimeoutTask, out bool hasTaskToAwaitCompleted
				, out TResult taskToAwaitResult
			)
			{
				if (_isVoid)
				{
					hasTaskToAwaitCompleted = ((Task<bool>)tryAwaitOrTimeoutTask).Result;
					taskToAwaitResult = default;
				}
				else
					(hasTaskToAwaitCompleted, taskToAwaitResult) = ((Task<ValueTuple<bool, TResult>>)tryAwaitOrTimeoutTask).Result;
			}

			public void CompleteTaskSuccessfully()
			{
				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();
				_taskToAwaitTcs.SetResult(_resultValue);
			}

			public void VerifyTryAwaitCompletedSuccessfully(Task tryAwaitOrTimeoutTask, Task delayTask)
			{
				tryAwaitOrTimeoutTask.IsCompletedSuccessfully().Should().BeTrue();

				UnpackTryAwaitOrTimeoutTaskResult(tryAwaitOrTimeoutTask, out var hasTaskToAwaitCompleted, out var taskToAwaitResult);
				hasTaskToAwaitCompleted.Should().BeTrue();
				taskToAwaitResult.Should().Be(_resultValue);

				_taskToAwaitTcs.Task.IsCompletedSuccessfully().Should().BeTrue();
				_taskToAwaitTcs.Task.Result.Should().Be(_resultValue);

				VerifyFinalAgentTimerState(delayTask);
			}

			public void VerifyAwaitCompletedSuccessfully(Task awaitOrTimeoutTask, Task delayTask)
			{
				awaitOrTimeoutTask.IsCompletedSuccessfully().Should().BeTrue();

				if (!_isVoid) ((Task<TResult>)awaitOrTimeoutTask).Result.Should().Be(_resultValue);

				_taskToAwaitTcs.Task.IsCompletedSuccessfully().Should().BeTrue();
				_taskToAwaitTcs.Task.Result.Should().Be(_resultValue);

				VerifyFinalAgentTimerState(delayTask);
			}

			public void VerifyTryAwaitTimeout(Task tryAwaitOrTimeoutTask, Task delayTask)
			{
				tryAwaitOrTimeoutTask.IsCompletedSuccessfully().Should().BeTrue();

				UnpackTryAwaitOrTimeoutTaskResult(tryAwaitOrTimeoutTask, out var hasTaskToAwaitCompleted, out var taskToAwaitResult);
				hasTaskToAwaitCompleted.Should().BeFalse();
				taskToAwaitResult.Should().Be(default(TResult));

				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();

				VerifyFinalAgentTimerState(delayTask, /* wasDelayCancelled: */ false);
			}

			public void VerifyAwaitTimeout(Task awaitOrTimeoutTask, Task delayTask)
			{
				awaitOrTimeoutTask.IsFaulted.Should().BeTrue();
				awaitOrTimeoutTask.Exception.InnerExceptions.Should().ContainSingle();
				awaitOrTimeoutTask.Exception.InnerException.Should().BeOfType<TimeoutException>();

				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();

				VerifyFinalAgentTimerState(delayTask, /* wasDelayCancelled: */ false);
			}

			public void CancelTask()
			{
				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();
				var trySetCanceledRetVal = _taskToAwaitTcs.TrySetCanceled(_cancellationToken);
				trySetCanceledRetVal.Should().BeTrue();
			}

			public void VerifyCancelled(Task xyzAwaitOrTimeoutTask, Task delayTask)
			{
				xyzAwaitOrTimeoutTask.IsCanceled.Should().BeTrue();
				// ReSharper disable once PossibleNullReferenceException
				OperationCanceledException ex = null;
				try
				{
					// ReSharper disable once MethodSupportsCancellation
					xyzAwaitOrTimeoutTask.Wait();
				}
				catch (AggregateException caughtEx)
				{
					caughtEx.InnerExceptions.Should().ContainSingle();
					ex = (OperationCanceledException)caughtEx.InnerException;
				}
				// ReSharper disable once PossibleNullReferenceException
				ex.CancellationToken.Should().Be(_cancellationToken);

				_taskToAwaitTcs.Task.IsCanceled.Should().BeTrue();

				VerifyFinalAgentTimerState(delayTask);
			}

			public void FaultTask()
			{
				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();
				_taskToAwaitTcs.SetException(_dummyTestException);
			}

			public void VerifyFaulted(Task xyzAwaitOrTimeoutTask, Task delayTask)
			{
				xyzAwaitOrTimeoutTask.IsFaulted.Should().BeTrue();
				// ReSharper disable once PossibleNullReferenceException
				xyzAwaitOrTimeoutTask.Exception.InnerException.Should().Be(_dummyTestException);

				_taskToAwaitTcs.Task.IsFaulted.Should().BeTrue();

				VerifyFinalAgentTimerState(delayTask);
			}

			public void VerifyDelayCancelled(Task xyzAwaitOrTimeoutTask, Task delayTask)
			{
				xyzAwaitOrTimeoutTask.IsCanceled.Should().BeTrue();

				_taskToAwaitTcs.Task.IsCompleted.Should().BeFalse();

				VerifyFinalAgentTimerState(delayTask);
			}

			private void VerifyFinalAgentTimerState(Task delayTask, bool wasDelayCancelled = true)
			{
				AgentTimer.PendingDelayTasksCount.Should().Be(0);
				if (wasDelayCancelled)
					delayTask.IsCanceled.Should().BeTrue();
				else
					delayTask.IsCompletedSuccessfully().Should().BeTrue();
			}
		}
	}
}
