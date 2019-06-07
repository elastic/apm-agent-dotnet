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

		private double _prevIdleTime;
		private double _prevTotalTime;
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
						if (firstLine == null || !firstLine.ToLower().StartsWith("cpu")) return null;

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

			return null;
		}
	}
}
