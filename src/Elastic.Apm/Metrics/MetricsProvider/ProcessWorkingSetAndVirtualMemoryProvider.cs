using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class ProcessWorkingSetAndVirtualMemoryProvider : IMetricsProvider
	{
		internal const string ProcessVirtualMemory = "system.process.memory.size";
		internal const string ProcessWorkingSetMemory = "system.process.memory.rss.bytes";

		private readonly bool _collectProcessVirtualMemory;
		private readonly bool _collectProcessWorkingSetMemory;

		public ProcessWorkingSetAndVirtualMemoryProvider(bool collectProcessVirtualMemory, bool collectProcessWorkingSetMemory) =>
			(_collectProcessVirtualMemory, _collectProcessWorkingSetMemory) = (collectProcessVirtualMemory, collectProcessWorkingSetMemory);

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "process's working set and virtual memory size";

		public IEnumerable<MetricSample> GetSamples()
		{
			var process = Process.GetCurrentProcess();
			var virtualMemory = process.VirtualMemorySize64;
			var workingSet = process.WorkingSet64;

			var retVal = new List<MetricSample>();

			if (_collectProcessVirtualMemory)
			{
				if (virtualMemory != 0)
					retVal.Add(new MetricSample(ProcessVirtualMemory, virtualMemory));
			}

			if (_collectProcessWorkingSetMemory)
			{
				if (workingSet != 0)
					retVal.Add(new MetricSample(ProcessWorkingSetMemory, workingSet));
			}

			return retVal;
		}
	}
}
