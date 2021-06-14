﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Docker.Tests
{
	public class BasicDockerTests
	{
		[RunningInDockerFact]
		public void ContainerIdExistsTest()
		{
			using var agent = new ApmAgent(new AgentComponents());
			var payloadSender = agent.PayloadSender as PayloadSenderV2;
			payloadSender.Should().NotBeNull();
			payloadSender?.System.Should().NotBeNull();
			payloadSender?.System.Container.Should().NotBeNull();
			payloadSender?.System.Container.Id.Should().NotBeNullOrWhiteSpace();
		}
	}
}
