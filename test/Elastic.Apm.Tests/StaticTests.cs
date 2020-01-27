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

			using var agentComponents = new AgentComponents();
			Agent.Setup(agentComponents);
			Agent.IsConfigured.Should().BeTrue();
		}
	}
}
