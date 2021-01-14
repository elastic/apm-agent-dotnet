// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// These tests access the static <see cref="Agent"/> instance.
	/// All other tests should have their own <see cref="ApmAgent"/> instance and not rely on anything static.
	/// Tests accessing the static <see cref="Agent"/> instance cannot run in parallel with tests that also access the static instance.
	/// </summary>
	public class StaticAgentTests
	{
		/// <summary>
		/// Makes sure Agent.IsConfigured only returns true after Setup is called.
		/// </summary>
		[Fact]
		public void IsConfigured()
		{
			Agent.IsConfigured.Should().BeFalse();

			using var agentComponents = new TestAgentComponents();
			Agent.Setup(agentComponents);
			Agent.IsConfigured.Should().BeTrue();
		}
	}
}
