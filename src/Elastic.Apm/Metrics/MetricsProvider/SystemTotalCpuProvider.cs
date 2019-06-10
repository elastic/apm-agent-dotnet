using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class SystemTotalCpuProvider : IMetricsProvider
	{
		private const string SystemCpuTotalPct = "system.cpu.total.norm.pct";

		public SystemTotalCpuProvider()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

			var procStatValues = ReadProcStat();
			if (!procStatValues.success) return;

			_prevIdleTime = procStatValues.idle;
			_prevTotalTime = procStatValues.total;
		}

		private long _prevIdleTime;
		private long _prevTotalTime;
		private PerformanceCounter _processorTimePerfCounter;

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "total system CPU time";

		internal (bool success, long idle, long total) ReadProcStat()
		{
			using (var sr = GetProcStatAsStream())
			{
				if (sr == null)
					return (false, 0, 0);

				var firstLine = sr.ReadLine();
				if (firstLine == null || !firstLine.ToLower().StartsWith("cpu")) return (false, 0 ,0);

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

		protected virtual StreamReader GetProcStatAsStream()
			=> File.Exists("/proc/stat") ? new StreamReader("/proc/stat") : null;

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
	}
}
