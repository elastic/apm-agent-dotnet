using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Timers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm.Metrics
{
	public class MetricsCollector : IDisposable
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
			IApmLogger logger1 = logger.Scoped(nameof(MetricsCollector));
			logger1.Debug()?.Log("starting metricscollector");

			_timer.Elapsed += (sender, args) =>
			{
				try
				{
					var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime; // .Milliseconds % 100 / 100;

					var virtualMemory = Process.GetCurrentProcess().VirtualMemorySize64;
					var workingSet = Process.GetCurrentProcess().WorkingSet64;


					var timespan = DateTimeOffset.UtcNow;

					if (!_first)
					{
						var cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;
						var totalMsPassed = (timespan - _lastTick).TotalMilliseconds;

						var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) / 100;

						Console.WriteLine($"cpuUsage: {cpuUsage}, cpuUsedMs: {cpuUsedMs}, cpuUsageTotal: {cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)},  Pid: {Process.GetCurrentProcess().Id}");

						var samples = new List<Sample>();

						var (isTotalMemoryAvailable, totalMemoryValue) = GetTotalMemory();
						if (isTotalMemoryAvailable) samples.Add(new Sample(_totalMemory, totalMemoryValue));

						var (isFreeMemoryAvailable, freeMemoryValue) = GetFreeMemory();
						if (isFreeMemoryAvailable) samples.Add(new Sample(_freeMemory, freeMemoryValue));

						var (isprocessorTimeAvailable, processorTimeValue) = GetTotalCpuTime();
						if (isprocessorTimeAvailable) samples.Add(new Sample(_systemCpuTotalPct, processorTimeValue));
						

						samples.Add(new Sample(_processCpuTotalPct, cpuUsageTotal));
						samples.Add(new Sample(_processWorkingSetMemory, workingSet));
						samples.Add(new Sample(_processVirtualMemory, virtualMemory));

						var metricSet = new Metrics(timespan.ToUnixTimeMilliseconds() * 1000, samples);
						payloadSender.QueueMetrics(metricSet);
						logger1.Debug()?.Log("Metrics collected");
					}

					_first = false;
					_lastTick = timespan;
					_lastCurrentProcessCpuTime = cpuUsage;
				}
				catch (Exception e)
				{
					logger1?.Error()?.LogExceptionWithCaller(e);
				}
			};

			_timer.Start();
		}

		private bool _first = true;
		private TimeSpan _lastCurrentProcessCpuTime;

		private DateTimeOffset _lastTick;

		private readonly ManagementObjectSearcher _managementObjectSearcher;
		private  PerformanceCounter _processorTimePerfCounter;

		private (bool, double) GetTotalCpuTime()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

			if(_processorTimePerfCounter == null)
				_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

			var val = _processorTimePerfCounter.NextValue();

			return (true, (double)val/100);
		}


		private PerformanceCounter _memoryPerfCounter;

		private (bool, ulong) GetFreeMemory()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

			if(_memoryPerfCounter == null)
				_memoryPerfCounter = new PerformanceCounter("Memory", "Available Bytes");

			var val = _memoryPerfCounter.NextValue();

			return (true, (ulong)val);
		}

		private (bool, ulong) GetTotalMemory()
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
