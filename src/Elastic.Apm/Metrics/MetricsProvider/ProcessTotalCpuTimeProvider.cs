using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class ProcessTotalCpuTimeProvider : IMetricsProvider
	{
		internal const string ProcessCpuTotalPct = "system.process.cpu.total.norm.pct";

		public ProcessTotalCpuTimeProvider(IApmLogger logger)
		{
			IApmLogger loggerInCtor = logger.Scoped(nameof(ProcessTotalCpuTimeProvider));

			IsMetricAlreadyCaptured = true;

			try
			{
				_lastTimeWindowStart = DateTime.UtcNow;
				_lastCurrentProcessCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
			}
			catch (Exception e)
			{
				loggerInCtor.Error()?.LogException(e, "Failed reading Process Total CPU Time");
			}
		}

		private TimeSpan _lastCurrentProcessCpuTime;
		private DateTime _lastTimeWindowStart;
		private Version _processAssemblyVersion;

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "process total CPU time";

		public IEnumerable<MetricSample> GetSamples()
		{
			// We have to make sure that the timespan of the wall clock time is the same or longer than the potential max possible CPU time -
			// we do this by the order in which we capture those.
			//
			// So:
			//
			//	Take timestamp
			//	Get CPU usage
			//
			//	do the work (in this case next call)
			//
			//	Get CPU usage
			//	Take timestamp
			//
			// Same as in https://github.com/dotnet/corefx/pull/37637#discussion_r283784218

			var timeWindowStart = DateTime.UtcNow;
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
			var timeWindowEnd = DateTime.UtcNow;

			if (_processAssemblyVersion == null)
				_processAssemblyVersion = typeof(Process).Assembly.GetName().Version;

			double cpuUsedMs;

			//workaround for a CoreFx bug. See: https://github.com/dotnet/corefx/issues/37614#issuecomment-492489373
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && _processAssemblyVersion < new Version(4, 3, 0))
				cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds / 100;
			else
				cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;

			var totalMsPassed = (timeWindowEnd - _lastTimeWindowStart).TotalMilliseconds;

			double cpuUsageTotal;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (totalMsPassed != 0)
				cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
			else
				cpuUsageTotal = 0;

			_lastTimeWindowStart = timeWindowStart;
			_lastCurrentProcessCpuTime = cpuUsage;
			return new List<MetricSample> { new MetricSample(ProcessCpuTotalPct, cpuUsageTotal) };
		}

		public bool IsMetricAlreadyCaptured { get; }
	}
}
