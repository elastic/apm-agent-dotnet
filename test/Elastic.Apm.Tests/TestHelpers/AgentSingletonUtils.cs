using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class AgentSingletonUtils
	{
		internal static void EnsureInstanceCreated(IApmLogger logger)
		{
			if (! Agent.IsInstanceCreated) Agent.Setup(new TestAgentComponents(logger));
		}
	}
}
