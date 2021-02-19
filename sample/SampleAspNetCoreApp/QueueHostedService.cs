// Sample taken from here:
// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio#queued-background-tasks

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleAspNetCoreApp
{
	public class QueuedHostedService : BackgroundService
	{
		private readonly ILogger<QueuedHostedService> _logger;

		public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
		{
			TaskQueue = taskQueue;
			_logger = logger;
		}

		public IBackgroundTaskQueue TaskQueue { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation(
				$"Queued Hosted Service is running.{Environment.NewLine}" +
				$"{Environment.NewLine}Tap W to add a work item to the " +
				$"background queue.{Environment.NewLine}");

			await BackgroundProcessing(stoppingToken);
		}

		private async Task BackgroundProcessing(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var workItem = await TaskQueue.DequeueAsync(stoppingToken);

				try
				{
					var tracingData = DistributedTracingData.TryDeserializeFromString(workItem.CorrelationId);
					await Agent.Tracer.CaptureTransaction("Background Task", ApiConstants.TypeExternal, () => workItem.Task(stoppingToken), tracingData);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
				}
			}
		}

		public override async Task StopAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Queued Hosted Service is stopping.");

			await base.StopAsync(stoppingToken);
		}
	}

	public interface IBackgroundTaskQueue
	{
		void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
		Task<WorkItem> DequeueAsync(CancellationToken cancellationToken);
	}

	public class BackgroundTaskQueue : IBackgroundTaskQueue
	{
		private readonly ConcurrentQueue<WorkItem> _workItems = new();
		private readonly SemaphoreSlim _signal = new(0);

		public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
		{
			if (workItem == null)
			{
				throw new ArgumentNullException(nameof(workItem));
			}

			_workItems.Enqueue(new WorkItem {
				Task = workItem,
				CorrelationId = Agent.Tracer.CurrentTransaction.OutgoingDistributedTracingData.SerializeToString()
			});
			_signal.Release();
		}

		public async Task<WorkItem> DequeueAsync(CancellationToken cancellationToken)
		{
			await _signal.WaitAsync(cancellationToken);
			_workItems.TryDequeue(out var workItem);

			return workItem;
		}
	}

	public class WorkItem
	{
		public Func<CancellationToken, Task> Task { get; set; }
		public string CorrelationId { get; set; }
	}
}
