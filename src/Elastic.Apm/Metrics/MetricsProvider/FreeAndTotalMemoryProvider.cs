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

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var retVal = new List<MetricSample>();

				using (var sr = new StreamReader("/proc/meminfo"))
				{
					var hasMemFree = false;
					var hasMemTotal = false;

					var line = sr.ReadLine();

					while (line != null || retVal.Count != 2)
					{
						//See: https://github.com/elastic/beats/issues/4202
						if (line != null && line.Contains("MemAvailable:") && _collectFreeMemory)
						{
							var (suc, res) = GetEntry(line, "MemAvailable:");
							if (suc) retVal.Add(new MetricSample(FreeMemory, res));
							hasMemFree = true;
						}
						if (line != null && line.Contains("MemTotal:") && _collectTotalMemory)
						{
							var (suc, res) = GetEntry(line, "MemTotal:");
							if (suc) retVal.Add(new MetricSample(TotalMemory, res));
							hasMemTotal = true;
						}

						if ((hasMemFree || !_collectFreeMemory) && (hasMemTotal || !_collectTotalMemory))
							break;

						line = sr.ReadLine();
					}
				}

				ConsecutiveNumberOfFailedReads = 0;

				return retVal;
			}

			(bool, ulong) GetEntry(string line, string name)
			{
				var nameIndex = line.IndexOf(name, StringComparison.Ordinal);
				if (nameIndex < 0)
					return (false, 0);

				var values = line.Substring(line.IndexOf(name, StringComparison.Ordinal) + name.Length);

				if (string.IsNullOrWhiteSpace(values)) return (false, 0);

				var items = values.Trim().Split(' ');

				switch (items.Length)
				{
					case 1 when ulong.TryParse(items[0], out var res): return (true, res);
					case 2 when items[1].ToLowerInvariant() == "kb" && ulong.TryParse(items[0], out var res): return (true, res * 1024);
					default: return (false, 0);
				}
			}

			return null;
		}
	}
}
