using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Apm.Helpers
{
	internal interface IAgentTimer
	{
		AgentTimeInstant Now { get; }

		Task Delay(AgentTimeInstant until, CancellationToken cancellationToken = default);
	}

	internal static class AgentTimerExtensions
	{
		/// <summary>
		/// It's recommended to use this method (or another TryAwaitOrTimeout or AwaitOrTimeout method)
		/// instead of just Task.WhenAny(taskToAwait, Task.Delay(timeout))
		/// because this method cancels the timer for timeout while <c>Task.Delay(timeout)</c>.
		/// If the number of “zombie” timer jobs starts becoming significant, performance could suffer.
		///
		/// For more detailed explanation see https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
		/// </summary>
		/// <returns><c>true</c> if <c>taskToAwait</c> completed before the timeout, <c>false</c> otherwise</returns>
		internal static async Task<bool> TryAwaitOrTimeout(this IAgentTimer agentTimer, Task taskToAwait
			, AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			var timeoutDelayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var timeoutDelayTask = agentTimer.Delay(until, timeoutDelayCts.Token);
			try
			{
				var completedTask = await Task.WhenAny(taskToAwait, timeoutDelayTask);
				if (completedTask == taskToAwait)
				{
					await taskToAwait;
					return true;
				}

				Assertion.IfEnabled?.That(completedTask == timeoutDelayTask
					, $"{nameof(completedTask)}: {completedTask}, {nameof(timeoutDelayTask)}: timeOutTask, {nameof(taskToAwait)}: taskToAwait");
				// no need to cancel timeout timer if it has been triggered
				timeoutDelayTask = null;
				return false;
			}
			finally
			{
				if (timeoutDelayTask != null) timeoutDelayCts.Cancel();
				timeoutDelayCts.Dispose();
			}
		}

		/// <summary>
		/// It's recommended to use this method (or another TryAwaitOrTimeout or AwaitOrTimeout method)
		/// instead of just Task.WhenAny(taskToAwait, Task.Delay(timeout))
		/// because this method cancels the timer for timeout while <c>Task.Delay(timeout)</c>.
		/// If the number of “zombie” timer jobs starts becoming significant, performance could suffer.
		///
		/// For more detailed explanation see https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
		/// </summary>
		/// <returns>(<c>true</c>, result of <c>taskToAwait</c>) if <c>taskToAwait</c> completed before the timeout, <c>false</c> otherwise</returns>
		internal static async Task<ValueTuple<bool, TResult>> TryAwaitOrTimeout<TResult>(this IAgentTimer agentTimer, Task<TResult> taskToAwait
			, AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			var hasTaskToAwaitCompletedBeforeTimeout =
				await TryAwaitOrTimeout(agentTimer, (Task)taskToAwait, until, cancellationToken);
			return (hasTaskToAwaitCompletedBeforeTimeout, hasTaskToAwaitCompletedBeforeTimeout ? await taskToAwait : default);
		}

		/// <summary>
		/// It's recommended to use this method (or another TryAwaitOrTimeout or AwaitOrTimeout method)
		/// instead of just Task.WhenAny(taskToAwait, Task.Delay(timeout))
		/// because this method cancels the timer for timeout while <c>Task.Delay(timeout)</c>.
		/// If the number of “zombie” timer jobs starts becoming significant, performance could suffer.
		///
		/// For more detailed explanation see https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
		/// </summary>
		/// <exception cref="TimeoutException">Thrown when timeout expires before <c>taskToAwait</c> completes</exception>
		internal static async Task AwaitOrTimeout(this IAgentTimer agentTimer, Task taskToAwait, AgentTimeInstant until
			, CancellationToken cancellationToken = default)
		{
			if (await TryAwaitOrTimeout(agentTimer, taskToAwait, until, cancellationToken)) return;
			throw new TimeoutException();
		}

		/// <summary>
		/// It's recommended to use this method (or another TryAwaitOrTimeout or AwaitOrTimeout method)
		/// instead of just Task.WhenAny(taskToAwait, Task.Delay(timeout))
		/// because this method cancels the timer for timeout while <c>Task.Delay(timeout)</c>.
		/// If the number of “zombie” timer jobs starts becoming significant, performance could suffer.
		///
		/// For more detailed explanation see https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
		/// </summary>
		/// <exception cref="TimeoutException">Thrown when timeout expires before <c>taskToAwait</c> completes</exception>
		/// <returns>(<c>true</c>, result of <c>taskToAwait</c>) if <c>taskToAwait</c> completed before the timeout, <c>false</c> otherwise</returns>
		internal static async Task<TResult> AwaitOrTimeout<TResult>(this IAgentTimer agentTimer, Task<TResult> taskToAwait
			, AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			var (hasTaskToAwaitCompletedBeforeTimeout, result) =
				await TryAwaitOrTimeout(agentTimer, taskToAwait, until, cancellationToken);
			if (hasTaskToAwaitCompletedBeforeTimeout) return result;
			throw new TimeoutException();
		}
	}
}
