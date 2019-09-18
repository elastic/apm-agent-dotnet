using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	//Credit: https://stackoverflow.com/a/30726903/1783306
	internal sealed class SingleThreadTaskScheduler : TaskScheduler
	{
		private const string ThisClassName = nameof(SingleThreadTaskScheduler);

		[ThreadStatic]
		private static bool _isExecuting;

		private readonly CancellationToken _cancellationToken;

		private readonly IApmLogger _logger;

		private readonly BlockingCollection<Task> _taskQueue;

		public SingleThreadTaskScheduler(string threadName, IApmLogger logger, CancellationToken cancellationToken)
		{
			_logger = logger?.Scoped(ThisClassName);
			_cancellationToken = cancellationToken;
			_taskQueue = new BlockingCollection<Task>();
			Thread = new Thread(RunOnCurrentThread) { Name = threadName, IsBackground = true };
			Thread.Start();
		}

		internal Thread Thread { get; }

		private void RunOnCurrentThread()
		{
			_logger.Debug()?.Log("`{ThreadName}' thread started", Thread.CurrentThread.Name);

			_isExecuting = true;

			ExceptionUtils.DoSwallowingExceptions(_logger, () =>
				{
					foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationToken)) TryExecuteTask(task);
				}
				, dbgCallerMethodName: $"`{Thread.CurrentThread.Name}' (ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}) thread");

			_isExecuting = false;
		}

		protected override IEnumerable<Task> GetScheduledTasks() => null;

		protected override void QueueTask(Task task) => _taskQueue.Add(task, _cancellationToken);

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			// We'd need to remove the task from queue if it was already queued.
			// That would be too hard.
			if (taskWasPreviouslyQueued) return false;

			return _isExecuting && TryExecuteTask(task);
		}
	}
}
