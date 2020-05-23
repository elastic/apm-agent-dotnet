// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Metrics.Windows;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// Returns total and free system memory.
	/// Currently Windows & Linux -only, no macOS support at the moment.
	/// </summary>
	internal class FreeAndTotalMemoryProvider : IMetricsProvider
	{
		internal const string FreeMemory = "system.memory.actual.free";
		internal const string TotalMemory = "system.memory.total";

		private readonly bool _collectFreeMemory;
		private readonly bool _collectTotalMemory;

		public FreeAndTotalMemoryProvider(bool collectFreeMemory, bool collectTotalMemory) =>
			(_collectFreeMemory, _collectTotalMemory, IsMetricAlreadyCaptured) = (collectFreeMemory, collectTotalMemory, true);

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "total and free memory";

		public bool IsMetricAlreadyCaptured { get; }

		public IEnumerable<MetricSample> GetSamples()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return GetSamplesForWindows();
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return GetSamplesForLinux();
			}

			return null;
		}

		private IEnumerable<MetricSample> GetSamplesForWindows()
		{
			var (success, totalMemory, freeMemory) = GlobalMemoryStatus.GetTotalPhysAndAvailPhys();

			if (!success || totalMemory == 0 || freeMemory == 0)
				return null;

			var retVal = new List<MetricSample>();

			if (_collectFreeMemory)
				retVal.Add(new MetricSample(FreeMemory, freeMemory));

			if (_collectTotalMemory)
				retVal.Add(new MetricSample(TotalMemory, totalMemory));

			return retVal;
		}

		private IEnumerable<MetricSample> GetSamplesForLinux()
		{
			var retVal = new List<MetricSample>();

			var (cGroupUsageInBytes, cGroupLimitInBytes) = GetCGroupMemoryInfo();
			var (procFreeMemory, procTotalMemory) = GetProcMemoryInfo();

			// cgroup limits are empty if container are running without limits
			var totalMemory = cGroupLimitInBytes ?? procTotalMemory;

			if (_collectTotalMemory && totalMemory.HasValue) retVal.Add(new MetricSample(TotalMemory, totalMemory.Value));

			var freeMemory = cGroupUsageInBytes.HasValue && totalMemory.HasValue
				? totalMemory.Value - cGroupUsageInBytes.Value
				: procFreeMemory;

			if (_collectFreeMemory && freeMemory.HasValue) retVal.Add(new MetricSample(FreeMemory, freeMemory.Value));

			ConsecutiveNumberOfFailedReads = 0;

			return retVal;

			(ulong?, ulong?) GetCGroupMemoryInfo()
			{
				// https://www.kernel.org/doc/Documentation/cgroup-v1/memory.txt
				const string usagePath = "/sys/fs/cgroup/memory/memory.usage_in_bytes";
				const string limitPath = "/sys/fs/cgroup/memory/memory.limit_in_bytes";

				ulong? usageInBytes = null;
				ulong? limitInBytes = null;


				if (_collectFreeMemory && File.Exists(usagePath) && ulong.TryParse(File.ReadAllText(usagePath), out var usage)) usageInBytes = usage;
				if (_collectTotalMemory && File.Exists(limitPath) && ulong.TryParse(limitPath, out var limit)) limitInBytes = limit;

				return (usageInBytes, limitInBytes);
			}

			(ulong?, ulong?) GetProcMemoryInfo()
			{
				ulong? memFree = 0;
				ulong? memTotal = 0;

				using (var sr = new StreamReader("/proc/meminfo"))
				{
					var hasMemFree = !_collectFreeMemory;
					var hasMemTotal = !_collectTotalMemory;

					var line = sr.ReadLine();

					while (line != null && (!hasMemFree || !hasMemTotal))
					{
						//See: https://github.com/elastic/beats/issues/4202
						if (!hasMemFree && line.Contains("MemAvailable:"))
						{
							var (suc, res) = GetEntry(line, "MemAvailable:");
							if (suc) memFree = res;
							hasMemFree = true;
						}
						if (!hasMemTotal && line.Contains("MemTotal:"))
						{
							var (suc, res) = GetEntry(line, "MemTotal:");
							if (suc) memTotal = res;
							hasMemTotal = true;
						}

						line = sr.ReadLine();
					}
				}

				return (memFree, memTotal);
			}

			static (bool, ulong) GetEntry(string line, string name)
			{
				var nameIndex = line.IndexOf(name, StringComparison.Ordinal);
				if (nameIndex < 0)
					return (false, 0);

				var values = line.Substring(line.IndexOf(name, StringComparison.Ordinal) + name.Length);

				if (string.IsNullOrWhiteSpace(values)) return (false, 0);

				var items = values.Trim().Split(' ');

				return items.Length switch
				{
					1 when ulong.TryParse(items[0], out var res) => (true, res),
					2 when items[1].ToLowerInvariant() == "kb" && ulong.TryParse(items[0], out var res) => (true, res * 1024),
					_ => (false, 0)
				};
			}
		}
	}
}
