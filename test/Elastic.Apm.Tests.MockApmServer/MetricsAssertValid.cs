using System;
using System.Collections.Generic;
using Elastic.Apm.Metrics.MetricsProvider;
using FluentAssertions;

namespace Elastic.Apm.Tests.MockApmServer
{
	internal static class MetricsAssertValid
	{
		internal static readonly Dictionary<string, MetricTypeMetadata> MetricMetadataPerType = new Dictionary<string, MetricTypeMetadata>()
		{
			{ FreeAndTotalMemoryProvider.FreeMemory, new MetricTypeMetadata(VerifyFreeMemory) },
			{ FreeAndTotalMemoryProvider.TotalMemory, new MetricTypeMetadata(VerifyTotalMemory) },
			{ ProcessTotalCpuTimeProvider.ProcessCpuTotalPct, new MetricTypeMetadata(VerifyProcessTotalCpu) },
			{ ProcessWorkingSetAndVirtualMemoryProvider.ProcessVirtualMemory, new MetricTypeMetadata(VerifyProcessVirtualMemory) },
			{ ProcessWorkingSetAndVirtualMemoryProvider.ProcessWorkingSetMemory, new MetricTypeMetadata(VerifyProcessWorkingSetMemory) },
			{ SystemTotalCpuProvider.SystemCpuTotalPct, new MetricTypeMetadata(VerifySystemTotalCpu, true) },
		};

		internal static void AssertValid(MetricSetDto metricSet)
		{
			metricSet.Should().NotBeNull();

			foreach (var metricSample in metricSet.Samples)
			{
				MetricMetadataPerType.Should().ContainKey(metricSample.Key);
				MetricMetadataPerType[metricSample.Key].VerifyAction(metricSample.Value.Value);
			}
		}

		private static void VerifyFreeMemory(double value) => value.Should().BeGreaterThan(0);

		private static void VerifyTotalMemory(double value) => value.Should().BeGreaterThan(1_000_000);

		private static void VerifyProcessTotalCpu(double value) => value.Should().BeInRange(0, 1);

		private static void VerifyProcessVirtualMemory(double value) => value.Should().BeGreaterThan(1_000_000);

		private static void VerifyProcessWorkingSetMemory(double value) => value.Should().BeGreaterThan(1_000_000);

		private static void VerifySystemTotalCpu(double value) => value.Should().BeInRange(0, 1);

		internal class MetricTypeMetadata
		{
			internal readonly bool ImplRequiresAccessToPerfCounters;

			internal readonly Action<double> VerifyAction;

			internal MetricTypeMetadata(Action<double> verifyAction, bool implRequiresAccessToPerfCounters = false)
			{
				VerifyAction = verifyAction;
				ImplRequiresAccessToPerfCounters = implRequiresAccessToPerfCounters;
			}
		}
	}
}
