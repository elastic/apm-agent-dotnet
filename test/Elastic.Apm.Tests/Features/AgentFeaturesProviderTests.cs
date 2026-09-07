// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Features;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Features;

// These tests call AgentFeaturesProvider.Create rather than Get on purpose. Get caches its result in a static field
// that any agent constructed by a test running in parallel populates from the real environment, so asserting on Get
// here would be a race. Create applies the same habitat detection against an explicit set of environment variables
// and leaves the cache alone.
public class AgentFeaturesProviderTests
{
	[Fact]
	public void Test_DefaultAgentFeatures()
	{
		var agentFeatures = AgentFeaturesProvider.Create(new NoopLogger(), new TestEnvironmentVariables());

		agentFeatures.Name.Should().Be("Default");
		agentFeatures.Check(AgentFeature.MetricsCollection).Should().BeTrue();
		agentFeatures.Check(AgentFeature.RemoteConfiguration).Should().BeTrue();
		agentFeatures.Check(AgentFeature.ContainerInfo).Should().BeTrue();
		agentFeatures.Check(AgentFeature.AzureFunctionsCloudMetaDataDiscovery).Should().BeFalse();
	}

	[Fact]
	public void Test_AzureFunctionsAgentFeatures()
	{
		var agentFeatures = AgentFeaturesProvider.Create(new NoopLogger(),
			new TestEnvironmentVariables { ["FUNCTIONS_WORKER_RUNTIME"] = "something" });

		agentFeatures.Name.Should().Be("Azure Functions");
		agentFeatures.Check(AgentFeature.MetricsCollection).Should().BeFalse();
		agentFeatures.Check(AgentFeature.RemoteConfiguration).Should().BeFalse();
		agentFeatures.Check(AgentFeature.ContainerInfo).Should().BeFalse();
		agentFeatures.Check(AgentFeature.AzureFunctionsCloudMetaDataDiscovery).Should().BeTrue();
	}
}
