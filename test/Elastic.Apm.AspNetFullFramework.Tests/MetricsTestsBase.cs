using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Metrics.MetricsProvider;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class MetricsTestsBase : TestsBase
	{
		private const string MetricsInterval = "1s"; // configure agent to send metrics every second

		private static readonly Dictionary<string, MetricTypeMetadata> MetricMetadataPerType = new Dictionary<string, MetricTypeMetadata>()
		{
			{ FreeAndTotalMemoryProvider.FreeMemory, new MetricTypeMetadata(VerifyFreeMemory) },
			{ FreeAndTotalMemoryProvider.TotalMemory, new MetricTypeMetadata(VerifyTotalMemory) },
			{ ProcessTotalCpuTimeProvider.ProcessCpuTotalPct, new MetricTypeMetadata(VerifyProcessTotalCpu) },
			{ ProcessWorkingSetAndVirtualMemoryProvider.ProcessVirtualMemory, new MetricTypeMetadata(VerifyProcessVirtualMemory) },
			{ ProcessWorkingSetAndVirtualMemoryProvider.ProcessWorkingSetMemory, new MetricTypeMetadata(VerifyProcessWorkingSetMemory) },
			{ SystemTotalCpuProvider.SystemCpuTotalPct, new MetricTypeMetadata(VerifySystemTotalCpu, true) },
		};

		protected MetricsTestsBase(ITestOutputHelper xUnitOutputHelper, bool sampleAppShouldHaveAccessToPerfCounters)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: BuildEnvVarsToSetForSampleAppPool(),
				sampleAppShouldHaveAccessToPerfCounters: sampleAppShouldHaveAccessToPerfCounters) { }

		protected async Task VerifyPeriodicallySentMetricsImpl()
		{
			// Send any request to the sample application to make sure it's running
			var sampleAppUrlPathData = RandomSampleAppUrlPath();
			await SendGetRequestToSampleAppAndVerifyResponseStatusCode(sampleAppUrlPathData.UrlPath, sampleAppUrlPathData.Status);

			VerifyPayloadFromAgent(receivedData =>
			{
				receivedData.Metrics.Should().NotBeEmpty();
				var samplesCountPerType = new Dictionary<string, int>();
				foreach (var metricSet in receivedData.Metrics)
				{
					foreach (var metricSample in metricSet.Samples)
					{
						MetricMetadataPerType.Should().ContainKey(metricSample.Key);
						MetricMetadataPerType[metricSample.Key].VerifyAction(metricSample.Value.Value);
						if (!samplesCountPerType.ContainsKey(metricSample.Key)) samplesCountPerType.Add(metricSample.Key, 0);
						++samplesCountPerType[metricSample.Key];
					}
				}

				foreach (var metricType in MetricMetadataPerType)
				{
					if (metricType.Value.ImplRequiresAccessToPerfCounters && !SampleAppShouldHaveAccessToPerfCounters)
						samplesCountPerType.Should().NotContainKey(metricType.Key);
					else
					{
						samplesCountPerType.Should().ContainKey(metricType.Key);
						samplesCountPerType[metricType.Key].Should().BePositive();
					}
				}
			});
		}

		private static Dictionary<string, string> BuildEnvVarsToSetForSampleAppPool() => new Dictionary<string, string>()
		{
			{ ConfigConsts.EnvVarNames.MetricsInterval, MetricsInterval }
		};

		private static void VerifyFreeMemory(double value) => value.Should().BeGreaterThan(0);

		private static void VerifyTotalMemory(double value) => value.Should().BeGreaterThan(1_000_000);

		private static void VerifyProcessTotalCpu(double value) => value.Should().BeInRange(0, 1);

		private static void VerifyProcessVirtualMemory(double value) => value.Should().BeGreaterThan(1_000_000);

		private static void VerifyProcessWorkingSetMemory(double value) => value.Should().BeGreaterThan(1_000_000);

		private static void VerifySystemTotalCpu(double value) => value.Should().BeInRange(0, 1);

		private class MetricTypeMetadata
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
