using System;
using System.Collections.Generic;
using System.ComponentModel;
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
			this._logger = logger.Scoped(nameof(MetricsCollector)); ;
			this._payloadSender = payloadSender;

			logger.Debug()?.Log("starting MetricsCollector");

			_timer.Elapsed += (sender, args) =>
			{
				CollectAllMetrics();
			};
		}

		internal void StartCollecting() => _timer.Start();

		private bool _first = true;
		private TimeSpan _lastCurrentProcessCpuTime;

		private DateTimeOffset _lastTick;

		private TimeSpan _lastCurrentTotalProcessCpuTime;

		private DateTimeOffset _lastTotalTick;

		private readonly IApmLogger _logger;
		private readonly IPayloadSender _payloadSender;
		private PerformanceCounter _processorTimePerfCounter;


		internal void CollectAllMetrics()
		{
			try
			{
				var samples = new List<Sample>();

				var workingSetAndVirtualMem = GetProcessWorkingSetAndVirtualMemory();
				if (workingSetAndVirtualMem != null)
					samples.AddRange(workingSetAndVirtualMem);

				var totalAndFreeMemory = GetTotalAndFreeMemoryMemory();
				if (totalAndFreeMemory != null)
					samples.AddRange(totalAndFreeMemory);

				var (isProcessorTimeAvailable, processorTimeValue) = GetTotalCpuTime();
				if (isProcessorTimeAvailable) samples.Add(new Sample(_systemCpuTotalPct, processorTimeValue));

				var (isProcessCpuAvailable, processCpuValue) = GetProcessTotalCpuTime();
				if (isProcessCpuAvailable) samples.Add(new Sample(_processCpuTotalPct, processCpuValue));

				var metricSet = new Metrics(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samples);
				_payloadSender.QueueMetrics(metricSet);
				_logger.Debug()?.Log("Metrics collected");
			}
			catch (Exception e)
			{
				_logger.Error()?.LogExceptionWithCaller(e);
			}
		}

		internal IEnumerable<Sample> GetProcessWorkingSetAndVirtualMemory()
		{
			var process = Process.GetCurrentProcess();
			var virtualMemory = process.VirtualMemorySize64;
			var workingSet = process.WorkingSet64;

			var retVal = new List<Sample>();

			if(virtualMemory != 0)
				 retVal.Add(new Sample(_processVirtualMemory, virtualMemory));

			if(workingSet != 0)
				retVal.Add(new Sample(_processWorkingSetMemory, workingSet));

			return retVal;
		}

		internal (bool, double) GetProcessTotalCpuTime()
		{
			var timespan = DateTimeOffset.UtcNow;
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

			if (!_first)
			{
				var cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;
				var totalMsPassed = (timespan - _lastTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);


				_first = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;
				return (true, cpuUsageTotal);
			}

			_first = false;
			_lastTick = timespan;
			_lastCurrentProcessCpuTime = cpuUsage;

			return (false, 0);
		}

		private bool _firstTotal = true;

		internal (bool, double) GetTotalCpuTime()
		{
			//Perf data:    CollectTotalCpuTime2X |    504.067 us | 143.7222 us |
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				if (_processorTimePerfCounter == null)
					_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

				var val = _processorTimePerfCounter.NextValue();

				return (true, (double)val / 100);
			}

			//The x-plat impelmentation is ~18x slower, than perf. counters on Windows
			//Therefore this is only a fallback for in case of non-Windows OSs
			var timespan = DateTimeOffset.UtcNow;
			TimeSpan cpuUsage;

			foreach (var proc in Process.GetProcesses())
			{
				try
				{
					cpuUsage += proc.TotalProcessorTime;
				}
				catch(Exception)
				{
					//Log warning 1 for inaccuracy
				}
			}

			if (!_firstTotal)
			{
				var cpuUsedMs = (cpuUsage - _lastCurrentTotalProcessCpuTime).TotalMilliseconds;
				var totalMsPassed = (timespan - _lastTotalTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

				_firstTotal = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;

				return (true, cpuUsageTotal);
			}

			_firstTotal = false;
			_lastTotalTick = timespan;
			_lastCurrentTotalProcessCpuTime = cpuUsage;

			return (false, 0);
		}


		//private PerformanceCounter _memoryPerfCounter;

		//internal (bool, ulong) GetFreeMemory()
		//{
		//	if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return (false, 0);

		//	if (_memoryPerfCounter == null)
		//		_memoryPerfCounter = new PerformanceCounter("Memory", "Available Bytes");

		//	var val = _memoryPerfCounter.NextValue();

		//	return (true, (ulong)val);
		//}

		internal IEnumerable<Sample> GetTotalAndFreeMemoryMemory()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;

			var (totalMemory, freeMemory) = Windows.GlobalMemoryStatus.GetTotalPhysAndAvailPhys();

			if (totalMemory == 0 || freeMemory == 0)
				return null;

			return new List<Sample>(2)
			{
				 new Sample(_freeMemory, freeMemory),
				 new Sample(_totalMemory, totalMemory)
			};
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
	}
}
