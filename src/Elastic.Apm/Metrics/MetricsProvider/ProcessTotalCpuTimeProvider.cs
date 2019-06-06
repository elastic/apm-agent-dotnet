using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	public class ProcessTotalCpuTimeProvider : IMetricsProvider
	{
		private const string ProcessCpuTotalPct = "system.process.cpu.total.norm.pct";
		private bool _first = true;
		private TimeSpan _lastCurrentProcessCpuTime;
		private DateTime _lastTick;

		private Version _processAssemblyVersion;

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "process total CPU time";

		public IEnumerable<MetricSample> GetSamples()
		{
			var timeStamp = DateTime.UtcNow;
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
			var currentTimeStamp = DateTime.UtcNow;

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

				var totalMsPassed = (currentTimeStamp - _lastTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);


				_first = false;
				_lastTick = timeStamp;
				_lastCurrentProcessCpuTime = cpuUsage;
				return new List<MetricSample> { new MetricSample(ProcessCpuTotalPct, cpuUsageTotal) };
			}

			_first = false;
			_lastTick = timeStamp;
			_lastCurrentProcessCpuTime = cpuUsage;

			return null;
		}
	}
}
