using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	public class SystemTotalCpuProvider : IMetricsProvider
	{
		private const string SystemCpuTotalPct = "system.cpu.total.norm.pct";
		private readonly IApmLogger _logger;

		public SystemTotalCpuProvider(IApmLogger logger)
		{
			_logger = logger.Scoped(nameof(SystemTotalCpuProvider));

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

			if (!File.Exists("/proc/stat")) return;

			using (var sr = new StreamReader("/proc/stat"))
			{
				var firstLine = sr.ReadLine();
				if (firstLine == null || !firstLine.ToLower().StartsWith("cpu")) return;

				var values = firstLine.Substring(5, firstLine.Length - 5).Split(' ');
				if (values.Length < 4)
					return;

				var numbers = new int[values.Length];

				if (values.Where((t, i) => !int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i])).Any())
					return;

				var total = numbers.Sum();
				var idle = numbers[3];

				_prevIdleTime = idle;
				_prevTotalTime = total;
			}
		}

		private bool _firstTotal = true;

		private bool _getTotalProcessorTimeFailedLog;
		private TimeSpan _lastCurrentTotalProcessCpuTime;
		private DateTime _lastTotalTick;
		private double _prevIdleTime;

		private double _prevTotalTime;
		private Version _processAssemblyVersion;
		private PerformanceCounter _processorTimePerfCounter;

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "total system CPU time";

		public IEnumerable<MetricSample> GetSamples()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				if (_processorTimePerfCounter == null)
					_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

				var val = _processorTimePerfCounter.NextValue();

				return new List<MetricSample> { new MetricSample(SystemCpuTotalPct, (double)val / 100) };
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				if (File.Exists("/proc/stat"))
				{
					using (var sr = new StreamReader("/proc/stat"))
					{
						var firstLine = sr.ReadLine();
						if (firstLine != null && firstLine.ToLower().StartsWith("cpu"))
						{
							var values = firstLine.Substring(5, firstLine.Length - 5).Split(' ');
							if (values.Length < 4)
								return null;

							var numbers = new int[values.Length];

							if (values.Where((t, i) => !int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i])).Any())
								return null;

							var total = numbers.Sum();
							var idle = (double)numbers[3];

							var idleTimeDelta = idle - _prevIdleTime;
							var totalTimeDelta = total - _prevTotalTime;

							var notIdle = 1.0 - idleTimeDelta / totalTimeDelta;

							_prevIdleTime = idle;
							_prevTotalTime = total;

							return new List<MetricSample> { new MetricSample(SystemCpuTotalPct, notIdle) };
						}
					}
				}
			}

			//The x-plat implementation is ~18x slower than perf. counters on Windows
			//Therefore this is only a fallback in case of non-Windows OSs
			//Also there are some issues with this code when e.g. the set of processes change between two runs.
			var timeSpan = DateTime.UtcNow;
			TimeSpan cpuUsage;

			foreach (var proc in Process.GetProcesses())
			{
				try
				{
					cpuUsage += proc.TotalProcessorTime;
				}
				catch (Exception)
				{
					//can happen if the current process does not have access to `proc`.
					if (_getTotalProcessorTimeFailedLog) continue;

					_getTotalProcessorTimeFailedLog = true;
					_logger.Info()
						?.Log(
							"The overall CPU usage reported by the agent may be inaccurate. This is because the agent was unable to access the CPU usage of some other processes");
				}
			}

			var currentTimeStamp = DateTime.UtcNow;

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

				var totalMsPassed = (currentTimeStamp - _lastTotalTick).TotalMilliseconds;

				if (totalMsPassed < 0)
					return null;

				double cpuUsageTotal;

				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (totalMsPassed != 0)
					cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
				else
					cpuUsageTotal = 0;

				_firstTotal = false;
				_lastTotalTick = timeSpan;
				_lastCurrentTotalProcessCpuTime = cpuUsage;

				return new List<MetricSample> { new MetricSample(SystemCpuTotalPct, cpuUsageTotal) };
			}

			_firstTotal = false;
			_lastTotalTick = timeSpan;
			_lastCurrentTotalProcessCpuTime = cpuUsage;

			return null;
		}
	}
}
