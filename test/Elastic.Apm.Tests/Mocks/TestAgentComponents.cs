// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.BackendComm;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestAgentComponents : AgentComponents
	{
		/// <inheritdoc />
		public TestAgentComponents(
			IApmLogger logger = null,
			IConfigSnapshot config = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer = null,
			ICentralConfigFetcher centralConfigFetcher = null,
			IApmServerInfo apmServerInfo = null
		) : base(
			logger ?? new NoopLogger(),
			config ?? new MockConfigSnapshot(logger ?? new NoopLogger()),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentsContainer,
			centralConfigFetcher ?? new NoopCentralConfigFetcher(),
			apmServerInfo ?? MockApmServerInfo.Version710
		)
		{ }
	}
}
