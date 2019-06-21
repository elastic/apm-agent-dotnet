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
	internal class SystemTotalCpuProvider : IMetricsProvider, IDisposable
	{
		private readonly IApmLogger _logger;
		internal const string SystemCpuTotalPct = "system.cpu.total.norm.pct";
		private readonly PerformanceCounter _processorTimePerfCounter;
		private readonly StreamReader _procStatStreamReader;

		public SystemTotalCpuProvider(IApmLogger logger)
		{
			_logger = logger.Scoped(nameof(SystemTotalCpuProvider));
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				try
				{
					_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
					//The perf. counter API returns 0 the for the 1. call (probably because there is no delta in the 1. call) - so we just call it here first
					_processorTimePerfCounter.NextValue();
				}
				catch (Exception e)
				{
					_logger.Error()
						?.LogException(e, "Failed instantiating PerformanceCounter "
							+ "- please make sure the current user has permissions to read performance counters. E.g. make sure the current user is member of "
							+ "the 'Performance Monitor Users' group");

					_processorTimePerfCounter?.Dispose();
					_processorTimePerfCounter = null;
				}
			}

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

			var procStatValues = ReadProcStat();
			if (!procStatValues.success) return;

			_prevIdleTime = procStatValues.idle;
			_prevTotalTime = procStatValues.total;
		}

		internal SystemTotalCpuProvider(IApmLogger logger, StreamReader procStatStreamReader)
			=> (_logger, _procStatStreamReader) = (logger.Scoped(nameof(SystemTotalCpuProvider)), procStatStreamReader);

		private long _prevIdleTime;
		private long _prevTotalTime;

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "total system CPU time";

		internal (bool success, long idle, long total) ReadProcStat()
		{
			using (var sr = GetProcStatAsStream())
			{
				if (sr == null)
					return (false, 0, 0);

				try
				{
					var firstLine = sr.ReadLine();
					if (firstLine == null || !firstLine.ToLower().StartsWith("cpu")) return (false, 0, 0);

					var values = firstLine.Substring(3, firstLine.Length - 3).Trim().Split(' ').ToArray();
					if (values.Length < 4)
						return (false, 0, 0);

					var numbers = new long[values.Length];

					if (values.Where((t, i) => !long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i])).Any())
						return (false, 0, 0);

					var total = numbers.Sum();
					var idle = numbers[3];

					return (true, idle, total);
				}
				catch
				{
					return (false, 0, 0);
				}
			}
		}

		private StreamReader GetProcStatAsStream()
			=> _procStatStreamReader ?? (File.Exists("/proc/stat") ? new StreamReader("/proc/stat") : null);

		public IEnumerable<MetricSample> GetSamples()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				if (_processorTimePerfCounter == null) return null;

				var val = _processorTimePerfCounter.NextValue();
				return new List<MetricSample> { new MetricSample(SystemCpuTotalPct, (double)val / 100) };
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var procStatValues = ReadProcStat();
				if (!procStatValues.success) return null;

				var idleTimeDelta = procStatValues.idle - _prevIdleTime;
				var totalTimeDelta = procStatValues.total - _prevTotalTime;
				var notIdle = 1.0 - idleTimeDelta / (double)totalTimeDelta;

				_prevIdleTime = procStatValues.idle;
				_prevTotalTime = procStatValues.total;

				return new List<MetricSample> { new MetricSample(SystemCpuTotalPct, notIdle) };
			}

			return null;
		}

		public void Dispose()
		{
			_procStatStreamReader?.Dispose();
			_processorTimePerfCounter?.Dispose();
		}
	}
}
