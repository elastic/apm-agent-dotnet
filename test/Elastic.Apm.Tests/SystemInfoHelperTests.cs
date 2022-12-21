// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api.Kubernetes;
using Elastic.Apm.Features;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

[Collection("Agent Features")]
public class SystemInfoHelperTests : IDisposable
{
	private readonly SystemInfoHelper _systemInfoHelper;

	public SystemInfoHelperTests() => _systemInfoHelper = new SystemInfoHelper(new NoopLogger());

	[Fact]
	public void ParseSystemInfo_Should_Use_HostName_For_ConfiguredHostName()
	{
		var hostName = "This_is_my_host";
		var system = _systemInfoHelper.GetSystemInfo(hostName);

#pragma warning disable 618
		system.HostName.Should().Be(hostName);
#pragma warning restore 618

		system.ConfiguredHostName.Should().Be(hostName);
		system.DetectedHostName.Should().NotBe(hostName);
	}

	[Fact]
	public void Feature_ContainerInfo_ShouldBeDisabled_OnAzure()
	{
		var logger = new TestLogger(LogLevel.Trace);
		using (new AgentFeaturesProviderScope(new AzureFunctionsAgentFeatures(logger)))
		{
			new SystemInfoHelper(logger).GetSystemInfo("bert");
			//
			// The actual parsing (not happening) is hard to test currently.
			// Let's assert the log output that tells us that this part gets skipped.
			//
			logger.Lines.Should().Contain(line => line.Contains("[Agent Feature] 'ContainerInfo' enabled: False"));
		}
	}

	[Fact]
	public void ParseKubernetesInfo_ShouldReturnNull_WhenNoEnvironmentVariablesAreSetAndContainerInfoIsNull()
	{
		// Arrange + Act
		var system = new Api.System();
		_systemInfoHelper.ParseKubernetesInfo(system);

		// Assert
		system.Kubernetes.Should().BeNull();
	}

	public struct CGroupTestData
	{
		public string GroupLine;
		public string ContainerId;
		public string PodId;
	}

// Remove warning about unused test parameter "name"
#pragma warning disable xUnit1026
	[Theory]
	[JsonFileData("./TestResources/json-specs/cgroup_parsing.json", typeof(CGroupTestData))]
	public void ParseKubernetesInfo_FromCGroupLine(string name, CGroupTestData data)
	{
		var line = data.GroupLine;
		var containerId = data.ContainerId;
		var podId = data.PodId;

		var system = new Api.System();
		_systemInfoHelper.ParseContainerId(system, "hostname", line);

		if (containerId is null)
			system.Container.Should().BeNull();
		else
			system.Container.Id.Should().Be(containerId);

		if (podId is null)
			system.Kubernetes.Should().BeNull();
		else
			system.Kubernetes.Pod.Uid.Should().Be(podId);
	}
#pragma warning restore xUnit1026

	[Fact]
	public void ParseKubernetesInfo_ShouldUseContainerInfoAndHostName_WhenNoEnvironmentVariablesAreSet()
	{
		// Arrange
		var podId = "e9b90526-f47d-11e8-b2a5-080027b9f4fb";
		var hostName = "hostName";
		var containerId = "15aa6e53-b09a-40c7-8558-c6c31e36c88a";
		var line = $"1:name=systemd:/kubepods/besteffort/pod{podId}/{containerId}";

		// Act
		var system = new Api.System();
		_systemInfoHelper.ParseContainerId(system, hostName, line);
		_systemInfoHelper.ParseKubernetesInfo(system);

		// Assert
		system.Container.Should().NotBeNull();
		system.Container.Id.Should().Be(containerId);
		system.Kubernetes.Should().NotBeNull();
		system.Kubernetes.Node.Should().BeNull();
		system.Kubernetes.Namespace.Should().BeNull();
		system.Kubernetes.Pod.Should().NotBeNull();
		system.Kubernetes.Pod.Uid.Should().Be(podId);
		system.Kubernetes.Pod.Name.Should().Be(hostName);
	}

	public static IEnumerable<object[]> EnvironmentVariablesData
	{
		get
		{
			const string podUid = "podUid";
			const string podName = "podName";
			const string nodeName = "nodeName";
			const string @namespace = "namespace";

			yield return new object[]
			{
				SystemInfoHelper.PodUid, podUid, new Action<KubernetesMetadata>(kubernetesInfo =>
				{
					kubernetesInfo.Node.Should().BeNull();
					kubernetesInfo.Namespace.Should().BeNull();

					kubernetesInfo.Pod.Should().NotBeNull();
					kubernetesInfo.Pod.Name.Should().BeNull();
					kubernetesInfo.Pod.Uid.Should().Be(podUid);
				})
			};
			yield return new object[]
			{
				SystemInfoHelper.PodName, podName, new Action<KubernetesMetadata>(kubernetesInfo =>
				{
					kubernetesInfo.Node.Should().BeNull();
					kubernetesInfo.Namespace.Should().BeNull();

					kubernetesInfo.Pod.Should().NotBeNull();
					kubernetesInfo.Pod.Uid.Should().BeNull();
					kubernetesInfo.Pod.Name.Should().Be(podName);
				})
			};
			yield return new object[]
			{
				SystemInfoHelper.NodeName, nodeName, new Action<KubernetesMetadata>(kubernetesInfo =>
				{
					kubernetesInfo.Pod.Should().BeNull();
					kubernetesInfo.Namespace.Should().BeNull();

					kubernetesInfo.Node.Should().NotBeNull();
					kubernetesInfo.Node.Name.Should().Be(nodeName);
				})
			};
			yield return new object[]
			{
				SystemInfoHelper.Namespace, @namespace, new Action<KubernetesMetadata>(kubernetesInfo =>
				{
					kubernetesInfo.Pod.Should().BeNull();
					kubernetesInfo.Node.Should().BeNull();

					kubernetesInfo.Namespace.Should().Be(@namespace);
				})
			};
		}
	}

	[Theory]
	[MemberData(nameof(EnvironmentVariablesData))]
	public void ParseKubernetesInfo_ShouldUseInformationFromEnvironmentVariables_WhenAtLeastOneVariableIsSet(
		string name, string value,
		Action<KubernetesMetadata> validateAction
	)
	{
		// Arrange
		Environment.SetEnvironmentVariable(name, value);

		// Act
		var system = new Api.System();
		_systemInfoHelper.ParseKubernetesInfo(system);

		// Assert
		system.Kubernetes.Should().NotBeNull();
		validateAction.Invoke(system.Kubernetes);
	}

	public void Dispose()
	{
		Environment.SetEnvironmentVariable(SystemInfoHelper.PodUid, null);
		Environment.SetEnvironmentVariable(SystemInfoHelper.PodName, null);
		Environment.SetEnvironmentVariable(SystemInfoHelper.NodeName, null);
		Environment.SetEnvironmentVariable(SystemInfoHelper.Namespace, null);
	}
}
