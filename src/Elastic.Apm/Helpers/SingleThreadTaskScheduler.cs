using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	//Credit: https://stackoverflow.com/a/30726903/1783306
	internal sealed class SingleThreadTaskScheduler : TaskScheduler, IDisposable
	{
		private const string ThisClassName = nameof(SingleThreadTaskScheduler);

		private readonly ThreadLocal<bool> _isExecuting = new ThreadLocal<bool>();

		private readonly IApmLogger _logger;

		private readonly string _threadName;
		private readonly Thread _thread;
		private readonly BlockingCollection<Task> _taskQueue;

		private readonly DisposableHelper _disposableHelper = new DisposableHelper();

		public SingleThreadTaskScheduler(string threadName, IApmLogger logger)
		{
			_threadName = threadName;
			_logger = logger?.Scoped(DbgName);
			_taskQueue = new BlockingCollection<Task>();
			_thread = new Thread(ThreadMain) { Name = threadName, IsBackground = true };
			_thread.Start();
		}

		private string DbgName => $"{ThisClassName} (thread: {_threadName})";

		internal bool IsRunning => _thread.IsAlive;

		private void ThreadMain()
		{
			_isExecuting.Value = true;

			ExceptionUtils.DoSwallowingExceptions(_logger, () =>
				{
					foreach (var task in _taskQueue.GetConsumingEnumerable()) TryExecuteTask(task);
				}
				, dbgCallerMethodName: $"{DbgUtils.CurrentThreadDesc} thread entry method");

			_isExecuting.Value = false;
		}

		/// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
		/// <returns>An enumerable of all tasks currently scheduled.</returns>
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			// Serialize the contents of the blocking collection of tasks for the debugger
			return _taskQueue.ToArray();
		}

		/// <summary>Queues a Task to be executed by this scheduler.</summary>
		/// <param name="task">The task to be executed.</param>
		protected override void QueueTask(Task task) => _taskQueue.Add(task);

		/// <summary>Determines whether a Task may be inlined.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
		/// <returns>true if the task was successfully inlined; otherwise, false.</returns>
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			// We'd need to remove the task from queue if it was already queued.
			// That would be too hard.
			if (taskWasPreviouslyQueued) return false;

			return _isExecuting.Value && TryExecuteTask(task);
		}

		/// <summary>
		/// Cleans up the scheduler by indicating that no more tasks will be queued.
		/// This method blocks until all threads successfully shutdown.
		/// </summary>
		public void Dispose() =>
			_disposableHelper.DoOnce(_logger, DbgName, () =>
			{
				// Indicate that no new tasks will be coming in
				_taskQueue.CompleteAdding();

				_logger.Context[DbgUtils.CurrentDbgContext(ThisClassName)] = "Before _thread.Join()... _thread.Name: `{_thread.Name}'."
					+ $"DbgCurrentState: {DbgCurrentState()}";

				_logger.Debug()?.Log("Waiting for thread `{ThreadName}' to exit... DbgCurrentState: {DbgCurrentState}", _thread.Name
					, DbgCurrentState());

				_thread.Join();

				_logger.Context[DbgUtils.CurrentDbgContext(ThisClassName)] = "Before _taskQueue.Dispose()... _thread.Name: `{_thread.Name}'.";

				_logger.Debug()?.Log("Disposing _taskQueue...");

				_taskQueue.Dispose();

				_logger.Debug()?.Log("Done");
			});

		private string DbgCurrentState() => new ToStringBuilder("")
		{
			{ "_thread.IsAlive", _thread.IsAlive },
			{ "_taskQueue", new ToStringBuilder("")
			{
				{ "IsCompleted", _taskQueue.IsCompleted },
				{ "IsAddingCompleted", _taskQueue.IsAddingCompleted },
				{ "Count", _taskQueue.Count }
			}.ToString() }
		}.ToString();
	}
}
