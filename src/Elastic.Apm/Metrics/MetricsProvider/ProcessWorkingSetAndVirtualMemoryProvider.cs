// Licensed to Elasticsearch B.V under
// one or more agreements.
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

		public ProcessWorkingSetAndVirtualMemoryProvider(IReadOnlyList<WildcardMatcher> disabledMetrics)
		{
			IsMetricAlreadyCaptured = true;
			_collectProcessVirtualMemory = IsProcessVirtualMemoryEnabled(disabledMetrics);
			_collectProcessWorkingSetMemory = IsProcessWorkingSetMemoryEnabled(disabledMetrics);
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "process's working set and virtual memory size";

		public bool IsMetricAlreadyCaptured { get; }

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			IsProcessVirtualMemoryEnabled(disabledMetrics) || IsProcessWorkingSetMemoryEnabled(disabledMetrics);

		private bool IsProcessVirtualMemoryEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			!WildcardMatcher.IsAnyMatch(disabledMetrics, ProcessVirtualMemory);

		private bool IsProcessWorkingSetMemoryEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			!WildcardMatcher.IsAnyMatch(disabledMetrics, ProcessWorkingSetMemory);

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
