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
		private static string _systemCpuTotalPct = "system.cpu.total.norm.pct";

		private static readonly string _totalMemory = "system.memory.total";

		private readonly Timer _timer = new Timer(100);

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

						var samples = new List<Sample>();

						var (isTotalMemoryAvailable, totalMemoryValue) = GetTotalMemory();
						if (isTotalMemoryAvailable) samples.Add(new Sample(_totalMemory, totalMemoryValue));

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

		private ManagementObjectSearcher _managementObjectSearcher;

		private (bool, ulong) GetTotalMemory()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

			if (_managementObjectSearcher == null)
				_managementObjectSearcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");

			ulong totalMemory = 0;
			foreach (var ram in _managementObjectSearcher.Get()) totalMemory += (ulong)ram.GetPropertyValue("Capacity");

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
