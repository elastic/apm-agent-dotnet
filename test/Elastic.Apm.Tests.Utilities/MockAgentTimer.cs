// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using FluentAssertions;

namespace Elastic.Apm.Tests.Utilities
{
	[DebuggerDisplay(nameof(_dbgName) + " = {" + nameof(_dbgName) + "}" + ", " + nameof(Now) + " = {" + nameof(Now) + "}")]
	internal class MockAgentTimer : IAgentTimerForTesting
	{
		private const string ThisClassName = nameof(MockAgentTimer);

		private readonly string _dbgName;
		private readonly DelayItems _delayItems = new DelayItems();
		private readonly AgentSpinLock _fastForwardSpinLock = new AgentSpinLock();

		private readonly IApmLogger _logger;

		internal MockAgentTimer(string dbgName = null, IApmLogger logger = null)
		{
			WhenStarted = new AgentTimeInstant(this, TimeSpan.Zero);
			Now = WhenStarted;
			_dbgName = dbgName ?? "#" + RuntimeHelpers.GetHashCode(this).ToString("X");
			_logger = logger == null ? (IApmLogger)new NoopLogger() : LoggingExtensions.Scoped(logger, $"{ThisClassName}-{_dbgName}");
		}

		public AgentTimeInstant Now
		{
			get => _delayItems.Now;

			private set => _delayItems.Now = value;
		}

		public IReadOnlyList<Task> PendingDelayTasks => _delayItems.DelayTasks;

		public int PendingDelayTasksCount => _delayItems.Count;

		internal AgentTimeInstant WhenStarted { get; }

		public Task Delay(AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			if (!until.IsCompatibleWith(this))
			{
				throw new ArgumentOutOfRangeException(nameof(until)
					, $"{nameof(until)} argument time instant should have this Agent timer as its source. {nameof(until)}: {until}");
			}

			return DelayAsyncImpl(until, cancellationToken);
		}

		private async Task DelayAsyncImpl(AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var triggerTcs = new TaskCompletionSource<object>();

			var (now, delayItemId) = _delayItems.Add(until, triggerTcs, cancellationToken);
			if (!delayItemId.HasValue)
			{
				LoggingExtensions.Trace(_logger)?.Log($"Delay item already reached its trigger time. When to trigger: {until}. now: {now}.");
				return;
			}

			LoggingExtensions.Trace(_logger)?.Log($"Added delay item. When to trigger: {until}. Delay item ID: {delayItemId.Value}");

			cancellationToken.Register(() => DelayCancelled(delayItemId.Value));

			await triggerTcs.Task;
		}

		internal void FastForward(TimeSpan timeSpanToFastForward, string dbgGoalDescription = null)
		{
			LoggingExtensions.Debug(_logger)?.Log($"Fast forwarding... timeSpanToFastForward: {timeSpanToFastForward}, dbgGoalDescription: {dbgGoalDescription}");

			using (var acq = _fastForwardSpinLock.TryAcquireWithDisposable())
			{
				if (!acq.IsAcquired)
					throw new InvalidOperationException($"{nameof(FastForward)} should not be called while the previous call is still in progress");

				var targetNow = Now + timeSpanToFastForward;

				while (true)
				{
					var delayItem = _delayItems.RemoveEarliestToTrigger(targetNow);
					if (!delayItem.HasValue) break;

					Assertion.IfEnabled?.That(delayItem.Value.WhenToTrigger >= Now, "Delay item should not have past trigger time");
					Now = delayItem.Value.WhenToTrigger;
					LoggingExtensions.Trace(_logger)?.Log($"Notifying delay item... Delay item ID: {delayItem.Value.Id}");
					delayItem.Value.TriggerTcs.SetResult(null);
				}

				Now = targetNow;
			}
		}

		public void WaitForTimeToPassAndUntil(TimeSpan timeSpan, Func<bool> untilCondition = null, Func<string> dbgDesc = null)
		{
			FastForward(timeSpan, dbgDesc?.Invoke());
			untilCondition?.Invoke().Should().BeTrue($"because dbgDesc: {ObjectExtensions.AsNullableToString((dbgDesc?.Invoke()))}");
		}

		private void DelayCancelled(long delayItemId)
		{
			var delayItem = _delayItems.RemoveById(delayItemId);
			if (!delayItem.HasValue)
			{
				LoggingExtensions.Debug(_logger)?.Log($"DelayItem with ID: {delayItemId} was not found (it's possible that it was already completed) - exiting");
				return;
			}

			LoggingExtensions.Trace(_logger)?.Log($"Cancelling delay item... Delay item ID: {delayItem.Value.Id}");
			var cancelled = delayItem.Value.TriggerTcs.TrySetCanceled(delayItem.Value.CancellationToken);
			Assertion.IfEnabled?.That(cancelled,
				$"Delay item task should not be in any final state before we cancel it because it was in {nameof(_delayItems)} list");
		}

		public override string ToString() =>
			new ToStringBuilder(ThisClassName) { { nameof(_dbgName), _dbgName }, { nameof(Now), Now } }.ToString();

		private readonly struct DelayItem
		{
			internal readonly long Id;
			internal readonly AgentTimeInstant WhenToTrigger;
			internal readonly TaskCompletionSource<object> TriggerTcs;
			internal readonly CancellationToken CancellationToken;

			internal DelayItem(long id, AgentTimeInstant whenToTrigger, TaskCompletionSource<object> triggerTcs, CancellationToken cancellationToken)
			{
				Id = id;
				WhenToTrigger = whenToTrigger;
				TriggerTcs = triggerTcs;
				CancellationToken = cancellationToken;
			}
		}

		private class DelayItems
		{
			private readonly List<DelayItem> _items = new List<DelayItem>();
			private readonly object _lock = new object();
			private long _nextItemId = 1;
			private AgentTimeInstant _now;

			internal int Count => DoUnderLock<int>(() => _items.Count);

			internal IReadOnlyList<Task> DelayTasks => DoUnderLock(() => _items.Select(d => d.TriggerTcs.Task).ToList());

			internal AgentTimeInstant Now
			{
				get => DoUnderLock<AgentTimeInstant>(() =>
				{
					var localCopy = _now;
					return localCopy;
				});

				set => DoUnderLock(() => { _now = value; });
			}

			internal (AgentTimeInstant, long?) Add(AgentTimeInstant whenToTrigger, TaskCompletionSource<object> triggerTcs,
				CancellationToken cancellationToken
			) =>
				DoUnderLock<(AgentTimeInstant _now, long?)>(() =>
				{
					if (whenToTrigger <= _now) return (_now, (long?)null);

					var newItemId = _nextItemId++;

					var newItemIndex = _items.TakeWhile(item => whenToTrigger >= item.WhenToTrigger).Count();
					_items.Insert(newItemIndex, new DelayItem(newItemId, whenToTrigger, triggerTcs, cancellationToken));
					return (_now, newItemId);
				});

			internal DelayItem? RemoveById(long itemId) =>
				DoUnderLock<DelayItem?>(() =>
				{
					var index = _items.FindIndex(delayItem => delayItem.Id == itemId);
					if (index == -1) return null;

					if (Assertion.IsEnabled && index != _items.Count - 1)
					{
						Assertion.IfEnabled?.That(_items.FindIndex(index + 1, delayItem => delayItem.Id == itemId) == -1,
							"There should be at most one item with each ID");
					}

					return RemoveAtReturnCopy(index);
				});

			internal DelayItem? RemoveEarliestToTrigger(AgentTimeInstant whenToTrigger) =>
				DoUnderLock<DelayItem?>(() =>
				{
					if (_items.IsEmpty() || _items[0].WhenToTrigger > whenToTrigger) return null;

					return RemoveAtReturnCopy(0);
				});

			private void AssertValid()
			{
				if (!Assertion.IsEnabled) return;

				Assertion.IfEnabled?.That(Monitor.IsEntered(_lock), "Current thread should hold the lock");

				for (var i = 0; i < _items.Count - 1; ++i)
				{
					Assertion.IfEnabled?.That(_items[i].WhenToTrigger <= _items[i + 1].WhenToTrigger,
						"Delay items should be in ascending order by trigger time");
				}
			}

			private TResult DoUnderLock<TResult>(Func<TResult> doFunc)
			{
				lock (_lock)
				{
					AssertValid();
					try
					{
						return doFunc();
					}
					finally
					{
						AssertValid();
					}
				}
			}

			private void DoUnderLock(Action doAction) =>
				DoUnderLock<object>(() =>
				{
					doAction();
					return (object)null;
				});

			private DelayItem RemoveAtReturnCopy(int index)
			{
				Assertion.IfEnabled?.That(Monitor.IsEntered(_lock), "Current thread should hold the lock");

				var itemCopy = _items[index];
				_items.RemoveAt(index);
				return itemCopy;
			}
		}
	}
}
