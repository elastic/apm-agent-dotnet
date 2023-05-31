using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.StaticExplicitInitialization.Tests
{
	public class StaticImplicitInitializationTests
	{
		/// <summary>
		/// Makes sure Agent.IsConfigured only returns true after Setup is called and
		/// makes sure a 2. call to Setup rejects the new <see cref="AgentComponents"/> instance.
		/// </summary>
		[Fact]
		public void ImplicitAgentInitialization()
		{
			Agent.IsConfigured.Should().BeFalse();

			var logger = new InMemoryBlockingLogger(LogLevel.Error);
			using var agentComponents = new TestAgentComponents(logger: logger);
			Agent.Setup(agentComponents);
			Agent.IsConfigured.Should().BeTrue();

			// 2. initialization with a new dummy-AgentComponents - this will be rejected
			Agent.Setup(new AgentComponents());

			Agent.Components.Should().Be(agentComponents);

			logger.Lines.Should()
				.Contain(n => n.Contains(
					"The singleton APM agent has already been instantiated and can no longer be configured. Reusing existing instance"));
		}
	}
}
