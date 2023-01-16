// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Apm.Cloud;
using Elastic.Apm.Features;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Tests.Cloud;

[Collection("Agent Features")]
public class CloudMetadataProviderCollectionTests
{
	[Fact]
	public void AzureFunctionsAgentFeature_Overrules_CloudProviderConfigValue()
	{
		var logger = new NoopLogger();
		using (new AgentFeaturesProviderScope(new AzureFunctionsAgentFeatures(logger)))
		{
			var providers = new CloudMetadataProviderCollection(DefaultValues.CloudProvider, logger);

			providers.Count.Should().Be(1);
			providers.First().Provider.Should().Be(AzureFunctionsMetadataProvider.Name);
		}
	}

	[Fact]
	public void DefaultCloudProvider_Registers_Aws_Gcp_Azure_Providers()
	{
		var providers = new CloudMetadataProviderCollection(DefaultValues.CloudProvider, new NoopLogger());

		providers.Count.Should().Be(5);
		providers.Contains(AwsCloudMetadataProvider.Name).Should().BeTrue();
		providers.Contains(GcpCloudMetadataProvider.Name).Should().BeTrue();
		providers.Contains(AzureCloudMetadataProvider.Name).Should().BeTrue();
		providers.Contains(AzureAppServiceMetadataProvider.Name).Should().BeTrue();
		providers.Select(p => p.Provider).Should()
			.Equal("aws", "gcp", "azure", "azure-app-service", "azure-functions");
	}

	[Fact]
	public void CloudProvider_None_Should_Not_Register_Any_Providers()
	{
		var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderNone, new NoopLogger());
		providers.Count.Should().Be(0);
	}

	[Fact]
	public void CloudProvider_Aws_Should_Register_Aws_Provider()
	{
		var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderAws, new NoopLogger());
		providers.Count.Should().Be(1);
		var provider = providers[SupportedValues.CloudProviderAws];
		provider.Should().BeOfType<AwsCloudMetadataProvider>();
	}

	[Fact]
	public void CloudProvider_Gcp_Should_Register_Gcp_Provider()
	{
		var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderGcp, new NoopLogger());
		providers.Count.Should().Be(1);
		var provider = providers[SupportedValues.CloudProviderGcp];
		provider.Should().BeOfType<GcpCloudMetadataProvider>();
	}

	[Fact]
	public void CloudProvider_Azure_Should_Register_Azure_Providers()
	{
		var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderAzure, new NoopLogger());
		providers.Count.Should().Be(3);
		var provider = providers[SupportedValues.CloudProviderAzure];
		provider.Should().BeOfType<AzureCloudMetadataProvider>();

		provider = providers[AzureAppServiceMetadataProvider.Name];
		provider.Should().BeOfType<AzureAppServiceMetadataProvider>();

		provider = providers[AzureFunctionsMetadataProvider.Name];
		provider.Should().BeOfType<AzureFunctionsMetadataProvider>();
	}
}
