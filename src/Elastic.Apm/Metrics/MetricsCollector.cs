using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm.Metrics
{
	internal class MetricsCollector : IDisposable
	{
		private static readonly string _processCpuTotalPct = "system.process.cpu.total.norm.pct";
		private static readonly string _processVirtualMemory = "system.process.memory.size";

		private static readonly string _processWorkingSetMemory = "system.process.memory.rss.bytes";
		private static readonly string _systemCpuTotalPct = "system.cpu.total.norm.pct";

		private static readonly string _totalMemory = "system.memory.total";
		private static readonly string _freeMemory = "system.memory.actual.free";

		private readonly Timer _timer = new Timer(1000);

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender)
		{
			this.logger = logger.Scoped(nameof(MetricsCollector)); ;
			this.payloadSender = payloadSender;

			logger.Debug()?.Log("starting metricscollector");

			_timer.Elapsed += (sender, args) =>
			{
				CollectAllMetrics();
			};

			_timer.Start();

		}

		private bool _first = true;
		private TimeSpan _lastCurrentProcessCpuTime;

		private DateTimeOffset _lastTick;

		private readonly ManagementObjectSearcher _managementObjectSearcher;
		private readonly IApmLogger logger;
		private readonly IPayloadSender payloadSender;
		private PerformanceCounter _processorTimePerfCounter;


		internal void CollectAllMetrics()
		{
			try
			{
				var virtualMemory = Process.GetCurrentProcess().VirtualMemorySize64;
				var workingSet = Process.GetCurrentProcess().WorkingSet64;

				var samples = new List<Sample>();

				var (isTotalMemoryAvailable, totalMemoryValue) = GetTotalMemory();
				if (isTotalMemoryAvailable) samples.Add(new Sample(_totalMemory, totalMemoryValue));

				var (isFreeMemoryAvailable, freeMemoryValue) = GetFreeMemory();
				if (isFreeMemoryAvailable) samples.Add(new Sample(_freeMemory, freeMemoryValue));

				var (isprocessorTimeAvailable, processorTimeValue) = GetTotalCpuTime();
				if (isprocessorTimeAvailable) samples.Add(new Sample(_systemCpuTotalPct, processorTimeValue));

				var (isProcessCpuAvailable, processCpuValue) = GetProcessTotalCpuTime();
				if (isProcessCpuAvailable) samples.Add(new Sample(_processCpuTotalPct, processCpuValue));

				samples.Add(new Sample(_processWorkingSetMemory, workingSet));
				samples.Add(new Sample(_processVirtualMemory, virtualMemory));

				var metricSet = new Metrics(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samples);
				payloadSender.QueueMetrics(metricSet);
				logger.Debug()?.Log("Metrics collected");
			}
			catch (Exception e)
			{
				logger.Error()?.LogExceptionWithCaller(e);
			}
		}

		internal (bool, double) GetProcessTotalCpuTime()
		{
			var timespan = DateTimeOffset.UtcNow;
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

			if (_first)
			{
				var cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;
				var totalMsPassed = (timespan - _lastTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

				return (true, cpuUsageTotal);
			}

			_first = false;
			_lastTick = timespan;
			_lastCurrentProcessCpuTime = cpuUsage;

			return (false, 0);
		}

		internal (bool, double) GetTotalCpuTime()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

			if (_processorTimePerfCounter == null)
				_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

			var val = _processorTimePerfCounter.NextValue();

			return (true, (double)val / 100);
		}


		private PerformanceCounter _memoryPerfCounter;

		internal (bool, ulong) GetFreeMemory()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

			if (_memoryPerfCounter == null)
				_memoryPerfCounter = new PerformanceCounter("Memory", "Available Bytes");

			var val = _memoryPerfCounter.NextValue();

			return (true, (ulong)val);
		}

		internal (bool, ulong) GetTotalMemory()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

			var mc = new ManagementClass("Win32_ComputerSystem");
			var moc = mc.GetInstances();
			ulong totalMemory = 0;

			foreach (var item in moc)
			{
				if (item.Properties["TotalPhysicalMemory"] != null)
				{
					totalMemory = Convert.ToUInt64(item.Properties["TotalPhysicalMemory"].Value);
					break;
				}
			}

			return (true, totalMemory);
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
			_managementObjectSearcher?.Dispose();
		}
	}
}
