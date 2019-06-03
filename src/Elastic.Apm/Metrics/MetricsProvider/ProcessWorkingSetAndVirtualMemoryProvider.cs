using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	public class ProcessWorkingSetAndVirtualMemoryProvider : IMetricsProvider
	{
		private const string ProcessVirtualMemory = "system.process.memory.size";
		private const string ProcessWorkingSetMemory = "system.process.memory.rss.bytes";


		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string NameInLogs => "process's working set and virtual memory size";

		public IEnumerable<Sample> GetValue()
		{
			var process = Process.GetCurrentProcess();
			var virtualMemory = process.VirtualMemorySize64;
			var workingSet = process.WorkingSet64;

			var retVal = new List<Sample>();

			if (virtualMemory != 0)
				retVal.Add(new Sample(ProcessVirtualMemory, virtualMemory));

			if (workingSet != 0)
				retVal.Add(new Sample(ProcessWorkingSetMemory, workingSet));

			return retVal;
		}
	}
}
