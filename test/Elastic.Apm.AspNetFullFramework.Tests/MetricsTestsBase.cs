using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class MetricsTestsBase : TestsBase
	{
		private const int MetricsIntervalSeconds = 1; // configure agent to send metrics every second
		private static readonly string MetricsInterval = $"{MetricsIntervalSeconds}s";

		protected MetricsTestsBase(ITestOutputHelper xUnitOutputHelper, bool sampleAppShouldHaveAccessToPerfCounters)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string>() { { ConfigConsts.EnvVarNames.MetricsInterval, MetricsInterval } },
				sampleAppShouldHaveAccessToPerfCounters: sampleAppShouldHaveAccessToPerfCounters) { }

		protected async Task VerifyMetricsBasicConstraintsImpl()
		{
			// Send any request to the sample application to make sure it's running since IIS might start worker process lazily
			var sampleAppUrlPathData = RandomSampleAppUrlPath();
			await SendGetRequestToSampleAppAndVerifyResponse(sampleAppUrlPathData.RelativeUrlPath,
				sampleAppUrlPathData.StatusCode, /* timeHttpCall: */ false);

			// Wait enough time to give agent a chance to gather all the metrics
			await Task.Delay(2 * MetricsIntervalSeconds * 1000);

			await VerifyDataReceivedFromAgent(receivedData =>
			{
				receivedData.Metrics.Should().NotBeEmpty();
				var samplesCountPerType = new Dictionary<string, int>();
				foreach (var metricSet in receivedData.Metrics)
				{
					foreach (var metricSample in metricSet.Samples)
					{
						if (!samplesCountPerType.ContainsKey(metricSample.Key)) samplesCountPerType.Add(metricSample.Key, 0);
						++samplesCountPerType[metricSample.Key];
					}
				}

				foreach (var metricType in MetricsAssertValid.MetricMetadataPerType)
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
	}
}
