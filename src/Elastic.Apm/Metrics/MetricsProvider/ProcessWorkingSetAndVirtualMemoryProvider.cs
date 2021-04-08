// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class ProcessWorkingSetAndVirtualMemoryProvider : IMetricsProvider
	{
		internal const string ProcessVirtualMemory = "system.process.memory.size";
		internal const string ProcessWorkingSetMemory = "system.process.memory.rss.bytes";

		private readonly bool _collectProcessVirtualMemory;
		private readonly bool _collectProcessWorkingSetMemory;

		public ProcessWorkingSetAndVirtualMemoryProvider(bool collectProcessVirtualMemory, bool collectProcessWorkingSetMemory) =>
			(_collectProcessVirtualMemory, _collectProcessWorkingSetMemory, IsMetricAlreadyCaptured) =
			(collectProcessVirtualMemory, collectProcessWorkingSetMemory, true);

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "process's working set and virtual memory size";

		public bool IsMetricAlreadyCaptured { get; }

		public IEnumerable<MetricSet> GetSamples()
		{
			var process = Process.GetCurrentProcess();
			var virtualMemory = process.VirtualMemorySize64;
			var workingSet = process.WorkingSet64;

			var samples = new List<MetricSample>();

			if (_collectProcessVirtualMemory)
			{
				if (virtualMemory != 0)
					samples.Add(new MetricSample(ProcessVirtualMemory, virtualMemory));
			}

			if (_collectProcessWorkingSetMemory)
			{
				if (workingSet != 0)
					samples.Add(new MetricSample(ProcessWorkingSetMemory, workingSet));
			}

			return new List<MetricSet> { new MetricSet(TimeUtils.TimestampNow(), samples) };
		}
	}
}
