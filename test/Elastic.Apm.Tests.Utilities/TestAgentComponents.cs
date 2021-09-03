// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Tests.Utilities
{
	internal class TestAgentComponents : AgentComponents
	{
		/// <inheritdoc />
		public TestAgentComponents(
			IApmLogger logger = null,
			IConfigurationSnapshot configuration = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer = null,
			ICentralConfigurationFetcher centralConfigurationFetcher = null,
			IApmServerInfo apmServerInfo = null
		) : base(
			logger ?? new NoopLogger(),
			configuration ?? new MockConfigurationSnapshot(logger ?? new NoopLogger()),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentsContainer,
			centralConfigurationFetcher ?? new NoopCentralConfigurationFetcher(),
			apmServerInfo ?? MockApmServerInfo.Version710
		)
		{ }
	}
}
