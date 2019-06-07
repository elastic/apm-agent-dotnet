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

		private double _prevIdleTime;
		private double _prevTotalTime;
		private PerformanceCounter _processorTimePerfCounter;

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "total system CPU time";

		private static (bool success, double idle, int total) ReadProcStat()
		{
			if (!File.Exists("/proc/stat")) return (false, 0, 0);

			using (var sr = new StreamReader("/proc/stat"))
			{
				var firstLine = sr.ReadLine();
				if (firstLine == null || !firstLine.ToLower().StartsWith("cpu")) return (false, 0 ,0);

				var values = firstLine.Substring(5, firstLine.Length - 5).Split(' ');
				if (values.Length < 4)
					return (false, 0 ,0);

				var numbers = new int[values.Length];

				if (values.Where((t, i) => !int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i])).Any())
					return (false, 0 ,0);

				var total = numbers.Sum();
				var idle = numbers[3];

				return (true, idle, total);
			}
		}

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
				if (!File.Exists("/proc/stat")) return null;

				var procStatValues = ReadProcStat();
				if (!procStatValues.success) return null;

				var idleTimeDelta = procStatValues.idle - _prevIdleTime;
				var totalTimeDelta = procStatValues.total - _prevTotalTime;
				var notIdle = 1.0 - idleTimeDelta / totalTimeDelta;

				_prevIdleTime = procStatValues.idle;
				_prevTotalTime = procStatValues.total;

				return new List<MetricSample> { new MetricSample(SystemCpuTotalPct, notIdle) };
			}

			return null;
		}
	}
}
