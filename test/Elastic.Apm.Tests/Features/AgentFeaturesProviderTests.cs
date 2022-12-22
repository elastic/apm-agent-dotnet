// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Features;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Features;

[Collection("Agent Features")]
public class AgentFeaturesProviderTests
{
	[Fact]
	public void Test_DefaultAgentFeatures()
	{
		using (new AgentFeaturesProviderScope())
		{
			var agentFeatures = AgentFeaturesProvider.Get(new NoopLogger());
			agentFeatures.Name.Should().Be("Default");
			agentFeatures.Check(AgentFeature.MetricsCollection).Should().BeTrue();
			agentFeatures.Check(AgentFeature.RemoteConfiguration).Should().BeTrue();
			agentFeatures.Check(AgentFeature.ContainerInfo).Should().BeTrue();
			agentFeatures.Check(AgentFeature.AzureFunctionsCloudMetaDataDiscovery).Should().BeFalse();
		}
	}

	[Fact]
	public void Test_AzureFunctionsAgentFeatures()
	{
		using (new AgentFeaturesProviderScope())
		{
			var agentFeatures = AgentFeaturesProvider.Get(new NoopLogger(),
				new TestEnvironmentVariables { ["FUNCTIONS_WORKER_RUNTIME"] = "something" });
			agentFeatures.Name.Should().Be("Azure Functions");
			agentFeatures.Check(AgentFeature.MetricsCollection).Should().BeFalse();
			agentFeatures.Check(AgentFeature.RemoteConfiguration).Should().BeFalse();
			agentFeatures.Check(AgentFeature.ContainerInfo).Should().BeFalse();
			agentFeatures.Check(AgentFeature.AzureFunctionsCloudMetaDataDiscovery).Should().BeTrue();
		}
	}
}
