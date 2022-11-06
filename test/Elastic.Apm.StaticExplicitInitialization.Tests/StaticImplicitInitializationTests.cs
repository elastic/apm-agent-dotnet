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

			using var agentComponents = new TestAgentComponents();
			Agent.Setup(agentComponents);
			Agent.IsConfigured.Should().BeTrue();

			// 2. initialization with a new dummy-AgentComponents - this will be rejected
			var logger = new InMemoryBlockingLogger(LogLevel.Warning);
			using var secondAgentComponents = new TestAgentComponents(logger: logger);
			Agent.Setup(secondAgentComponents);

			Agent.Components.Should().NotBe(agentComponents);
			Agent.Components.Should().Be(secondAgentComponents);

			logger.Lines.Should()
				.Contain(n => n.Contains(
					"re-initialized"));
		}
	}
}
