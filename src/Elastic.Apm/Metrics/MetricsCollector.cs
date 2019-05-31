using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.Windows;
using Elastic.Apm.Report;

namespace Elastic.Apm.Metrics
{
	internal class MetricsCollector : IDisposable, IMetricsCollector
	{
		private const string FreeMemory = "system.memory.actual.free";
		private const string ProcessCpuTotalPct = "system.process.cpu.total.norm.pct";
		private const string ProcessVirtualMemory = "system.process.memory.size";

		private const string ProcessWorkingSetMemory = "system.process.memory.rss.bytes";
		private const string SystemCpuTotalPct = "system.cpu.total.norm.pct";

		private const string TotalMemory = "system.memory.total";

		private readonly object _lock = new object();

		private readonly IConfigurationReader _configurationReader;

		private readonly IApmLogger _logger;
		private readonly IPayloadSender _payloadSender;

		private readonly Timer _timer;

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender, IConfigurationReader configurationReader)
		{
			_logger = logger.Scoped(nameof(MetricsCollector));
			_payloadSender = payloadSender;

			logger.Debug()?.Log("starting MetricsCollector");

			_configurationReader = configurationReader;
			_timer = new Timer(configurationReader.MetricsIntervalInMillisecond);

			_timer.Elapsed += (sender, args) => { CollectAllMetrics(); };
		}

		private bool _first = true;

		private bool _firstTotal = true;
		private TimeSpan _lastCurrentProcessCpuTime;

		private TimeSpan _lastCurrentTotalProcessCpuTime;

		private DateTime _lastTick;

		private DateTime _lastTotalTick;
		private PerformanceCounter _processorTimePerfCounter;

		public void StartCollecting() => _timer.Start();

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
				{
					var andFreeMemory = totalAndFreeMemory as Sample[] ?? totalAndFreeMemory.ToArray();
					if (andFreeMemory.Any())
						samples.AddRange(andFreeMemory);
				}

				var (isSystemTotalCpuAvailable, systemTotalCpuTime) = GetSystemTotalCpuTime();
				if (isSystemTotalCpuAvailable) samples.Add(new Sample(SystemCpuTotalPct, systemTotalCpuTime));

				var (isProcessCpuAvailable, processCpuValue) = GetProcessTotalCpuTime();
				if (isProcessCpuAvailable) samples.Add(new Sample(ProcessCpuTotalPct, processCpuValue));

				var metricSet = new MetricSet(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samples);
				_payloadSender.QueueMetrics(metricSet);
				_logger.Debug()?.Log("Metrics collected: {data}", samples.Select(n => n.ToString()).Aggregate((i, j) => i + ", " + j));
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

			if (virtualMemory != 0)
				retVal.Add(new Sample(ProcessVirtualMemory, virtualMemory));

			if (workingSet != 0)
				retVal.Add(new Sample(ProcessWorkingSetMemory, workingSet));

			return retVal;
		}

		private Version _processAssemblyVersion;

		internal (bool, double) GetProcessTotalCpuTime()
		{
			//TODO: this can be wrong, see: https://github.com/dotnet/corefx/pull/37637#discussion_r283784218
			var timespan = DateTime.UtcNow;
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

			if (!_first)
			{
				if (_processAssemblyVersion == null)
					_processAssemblyVersion = typeof(Process).Assembly.GetName().Version;

				double cpuUsedMs;

				//workaround for a CoreFx bug. See: https://github.com/dotnet/corefx/issues/37614#issuecomment-492489373
				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && _processAssemblyVersion < new Version(4, 3, 0))
					cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds / 100;
				else
					cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;

				var totalMsPassed = (timespan - _lastTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);


				_first = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;
				return (true, cpuUsageTotal);
			}

			lock (_lock)
			{
				_first = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;
			}

			return (false, 0);
		}

		internal (bool, double) GetSystemTotalCpuTime()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				if (_processorTimePerfCounter == null)
					_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

				var val = _processorTimePerfCounter.NextValue();

				return (true, (double)val / 100);
			}

			//The x-plat implementation is ~18x slower, than perf. counters on Windows
			//Therefore this is only a fallback for in case of non-Windows OSs
			var timespan = DateTime.UtcNow;
			TimeSpan cpuUsage;

			foreach (var proc in Process.GetProcesses())
			{
				try
				{
					cpuUsage += proc.TotalProcessorTime;
				}
				catch (Exception)
				{
					//Log warning 1 for inaccuracy
				}
			}

			if (!_firstTotal)
			{
				if (_processAssemblyVersion == null)
					_processAssemblyVersion = typeof(Process).Assembly.GetName().Version;

				double cpuUsedMs;

				//workaround for a CoreFx bug. See: https://github.com/dotnet/corefx/issues/37614#issuecomment-492489373
				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && _processAssemblyVersion < new Version(4, 3, 0))
					cpuUsedMs = (cpuUsage - _lastCurrentTotalProcessCpuTime).TotalMilliseconds / 100;
				else
					cpuUsedMs = (cpuUsage - _lastCurrentTotalProcessCpuTime).TotalMilliseconds;

				var totalMsPassed = (timespan - _lastTotalTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

				_firstTotal = false;
				_lastTotalTick = timespan;
				_lastCurrentTotalProcessCpuTime = cpuUsage;

				return (true, cpuUsageTotal);
			}

			_firstTotal = false;
			_lastTotalTick = timespan;
			_lastCurrentTotalProcessCpuTime = cpuUsage;

			return (false, 0);
		}

		internal IEnumerable<Sample> GetTotalAndFreeMemoryMemory()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var (success, totalMemory, freeMemory) = GlobalMemoryStatus.GetTotalPhysAndAvailPhys();

				if (!success || totalMemory == 0 || freeMemory == 0)
					return null;

				return new List<Sample>(2) { new Sample(FreeMemory, freeMemory), new Sample(TotalMemory, totalMemory) };
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var retVal = new List<Sample>();

				try
				{
					using (var sr = new StreamReader("/proc/meminfo"))
					{
						var line = sr.ReadLine();

						while (line != null || retVal.Count != 2) //TODO: break early if possible
						{
							if (line != null && line.Contains("MemFree:"))
							{
								var (suc, res) = GetEntry(line, "MemFree:");
								if (suc) retVal.Add(new Sample(FreeMemory, res));
							}
							if (line != null && line.Contains("MemTotal:"))
							{
								var (suc, res) = GetEntry(line, "MemTotal:");
								if (suc) retVal.Add(new Sample(TotalMemory, res));
							}

							line = sr.ReadLine();
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"Exception: {e.GetType()} - {e.Message}"); //TODO!!
				}

				return retVal;
			}

			(bool, ulong) GetEntry(string line, string name)
			{
				var nameIndex = line.IndexOf(name, StringComparison.Ordinal);
				if (nameIndex < 0)
					return (false, 0);

				var values = line.Substring(line.IndexOf(name, StringComparison.Ordinal) + name.Length);

				if (string.IsNullOrWhiteSpace(values)) return (false, 0);

				var items = values.Trim().Split(' ');

				switch (items.Length)
				{
					case 1 when ulong.TryParse(items[0], out var res): return (true, res);
					case 2 when items[1].ToLower() == "kb" && ulong.TryParse(items[0], out var res): return (true, res * 1024);
					default: return (false, 0);
				}
			}

			return null;
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
	}
}
