using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class AgentSingletonUtils
	{
		internal static void EnsureInstanceCreated()
		{
			if (!Agent.IsInstanceCreated) Agent.Setup(new TestAgentComponents());
		}
	}
}
