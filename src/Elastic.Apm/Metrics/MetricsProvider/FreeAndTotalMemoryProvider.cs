using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Metrics.Windows;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class FreeAndTotalMemoryProvider : IMetricsProvider
	{
		private const string FreeMemory = "system.memory.actual.free";
		private const string TotalMemory = "system.memory.total";

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string NameInLogs => "total and free memory";

		public IEnumerable<Sample> GetValue()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var (success, totalMemory, freeMemory) = GlobalMemoryStatus.GetTotalPhysAndAvailPhys();

				if (!success || totalMemory == 0 || freeMemory == 0)
					return null;

				return new List<Sample>(2) { new Sample(FreeMemory, freeMemory), new Sample(TotalMemory, totalMemory) };
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var retVal = new List<Sample>();

				using (var sr = new StreamReader("/proc/meminfo"))
				{
					var hasMemFree = false;
					var hasMemTotal = false;

					var line = sr.ReadLine();

					while (line != null || retVal.Count != 2)
					{
						if (line != null && line.Contains("MemFree:"))
						{
							var (suc, res) = GetEntry(line, "MemFree:");
							if (suc) retVal.Add(new Sample(FreeMemory, res));
							hasMemFree = true;
						}
						if (line != null && line.Contains("MemTotal:"))
						{
							var (suc, res) = GetEntry(line, "MemTotal:");
							if (suc) retVal.Add(new Sample(TotalMemory, res));
							hasMemTotal = true;
						}

						if (hasMemFree && hasMemTotal)
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
					case 2 when items[1].ToLower() == "kb" && ulong.TryParse(items[0], out var res): return (true, res * 1024);
					default: return (false, 0);
				}
			}

			return null;
		}
	}
}
