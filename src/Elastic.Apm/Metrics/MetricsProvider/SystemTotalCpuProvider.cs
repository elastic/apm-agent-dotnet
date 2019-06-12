using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class SystemTotalCpuProvider : IMetricsProvider, IDisposable
	{
		private const string SystemCpuTotalPct = "system.cpu.total.norm.pct";
//		private readonly PerformanceCounter _processorTimePerfCounter;
		private readonly StreamReader _procStatStreamReader;

		public SystemTotalCpuProvider()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
//				_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
//				//The perf. counter API returns 0 the for the 1. call (probably because there is no delta in the 1. call) - so we just call it here first
//				_processorTimePerfCounter.NextValue();
			}

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

			var procStatValues = ReadProcStat();
			if (!procStatValues.success) return;

			_prevIdleTime = procStatValues.idle;
			_prevTotalTime = procStatValues.total;
		}

		internal SystemTotalCpuProvider(StreamReader procStatStreamReader) => _procStatStreamReader = procStatStreamReader;

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

				var firstLine = sr.ReadLine();
				if (firstLine == null || !firstLine.ToLowerInvariant().StartsWith("cpu")) return (false, 0, 0);

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
		}

		private StreamReader GetProcStatAsStream()
			=> _procStatStreamReader ?? (File.Exists("/proc/stat") ? new StreamReader("/proc/stat") : null);

		public IEnumerable<MetricSample> GetSamples()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
//				var val = _processorTimePerfCounter.NextValue();
				var val = Helpers.RandomGenerator.GenerateRandomDoubleBetween0And1() * 100;
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
//			_processorTimePerfCounter?.Dispose();
		}
	}
}
