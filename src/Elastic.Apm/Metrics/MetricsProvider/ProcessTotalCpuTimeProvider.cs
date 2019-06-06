using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	public class ProcessTotalCpuTimeProvider : IMetricsProvider
	{
		private const string ProcessCpuTotalPct = "system.process.cpu.total.norm.pct";
		private TimeSpan _lastCurrentProcessCpuTime;
		private DateTime _lastTick;

		private Version _processAssemblyVersion;

		public ProcessTotalCpuTimeProvider()
		{
			_lastTick = DateTime.UtcNow;
			_lastCurrentProcessCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "process total CPU time";

		public IEnumerable<MetricSample> GetSamples() => null;

		public IEnumerable<MetricSample> GetSamples2(IApmLogger logger)
		{
			var timeStamp = DateTime.UtcNow;
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
			var currentTimeStamp = DateTime.UtcNow;

			logger.Debug()?.Log($"timeStamp: {timeStamp.ToString("s.ffff")}");
			logger.Debug()?.Log($"cpuUsage: {cpuUsage}");
			logger.Debug()?.Log($"currentTimeStamp: {currentTimeStamp.ToString("s.ffff")}");

			if (_processAssemblyVersion == null)
				_processAssemblyVersion = typeof(Process).Assembly.GetName().Version;

			double cpuUsedMs;

			//workaround for a CoreFx bug. See: https://github.com/dotnet/corefx/issues/37614#issuecomment-492489373
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && _processAssemblyVersion < new Version(4, 3, 0))
				cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds / 100;
			else
				cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;

			logger.Debug()?.Log($"cpuUsedMs: {cpuUsedMs} = cpuUsage {cpuUsage} - _lastCurrentProcessCpuTime {_lastCurrentProcessCpuTime}, totalms: {(cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds} ");

			var totalMsPassed = (currentTimeStamp - _lastTick).TotalMilliseconds;

			var s1 = currentTimeStamp.ToString("s.ffff");
			var s2 = _lastTick.ToString("s.ffff");

			logger.Debug()?.Log($"totalMsPassed {totalMsPassed} = currentTimeStamp: {s1} - _lastTick {s2}");

			logger.Debug()?.Log($"cpuUsedMs: {cpuUsedMs}");
			logger.Debug()?.Log($"totalMsPassed: {totalMsPassed}");

			double cpuUsageTotal;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (totalMsPassed != 0)
			{
				cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

				logger.Debug()?.Log($"cpuUsageTotal: {cpuUsageTotal} = cpuUsedMs {cpuUsedMs} / (Environment.ProcessorCount {Environment.ProcessorCount} *  totalMsPassed: {totalMsPassed} )");
			}

			else
				cpuUsageTotal = 0;

			_lastTick = timeStamp;
			_lastCurrentProcessCpuTime = cpuUsage;
			return new List<MetricSample> { new MetricSample(ProcessCpuTotalPct, cpuUsageTotal) };
		}
	}
}
