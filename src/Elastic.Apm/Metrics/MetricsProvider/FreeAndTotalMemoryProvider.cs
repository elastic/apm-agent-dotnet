// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// Returns total and free system memory.
	/// Currently Windows and Linux only, no macOS support at the moment.
	/// </summary>
	internal class FreeAndTotalMemoryProvider : IMetricsProvider
	{
		internal const string FreeMemory = "system.memory.actual.free";
		internal const string TotalMemory = "system.memory.total";

		private readonly bool _collectFreeMemory;
		private readonly bool _collectTotalMemory;
		private readonly IApmLogger _logger;
		private readonly string _pathPrefix;
		private readonly bool _ignoreOs;

		internal static FreeAndTotalMemoryProvider TestableFreeAndTotalMemoryProvider(IApmLogger logger,
			IReadOnlyList<WildcardMatcher> disabledMetrics, string pathPrefix, bool ignoreOs) =>
				new(logger, disabledMetrics, pathPrefix, ignoreOs);

		public FreeAndTotalMemoryProvider(IApmLogger logger, IReadOnlyList<WildcardMatcher> disabledMetrics) :
			this(logger, disabledMetrics, string.Empty)
		{ }

		private FreeAndTotalMemoryProvider(IApmLogger logger, IReadOnlyList<WildcardMatcher> disabledMetrics,
			string pathPrefix, bool ignoreOs = false)
		{
			IsMetricAlreadyCaptured = true;
			_collectFreeMemory = IsFreeMemoryEnabled(disabledMetrics);
			_collectTotalMemory = IsTotalMemoryEnabled(disabledMetrics);
			_logger = logger.Scoped(nameof(FreeAndTotalMemoryProvider));
			_pathPrefix = pathPrefix;
			_ignoreOs = ignoreOs;
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "total and free memory";

		public bool IsMetricAlreadyCaptured { get; }

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			IsFreeMemoryEnabled(disabledMetrics) || IsTotalMemoryEnabled(disabledMetrics);

		private static bool IsFreeMemoryEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			!WildcardMatcher.IsAnyMatch(disabledMetrics, FreeMemory);

		private static bool IsTotalMemoryEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			!WildcardMatcher.IsAnyMatch(disabledMetrics, TotalMemory);

		public IEnumerable<MetricSet> GetSamples()
		{
			yield return new(TimeUtils.TimestampNow(), GetSamplesCore());
		}

		private IEnumerable<MetricSample> GetSamplesCore()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var (success, total, available) = Windows.GlobalMemoryStatus.GetTotalPhysAndAvailPhys();

				if (success && total != 0 && available != 0)
				{
					if (_collectFreeMemory)
						yield return new MetricSample(FreeMemory, available);

					if (_collectTotalMemory)
						yield return new MetricSample(TotalMemory, total);
				}
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var (totalMemory, availableMemory) = Linux.GlobalMemoryStatus.GetTotalAndAvailableSystemMemory(_logger);

				if (_collectFreeMemory && availableMemory > -1)
					yield return new MetricSample(FreeMemory, availableMemory);

				if (_collectTotalMemory && totalMemory > -1)
					yield return new MetricSample(TotalMemory, totalMemory);
			}
		}
	}
}
