using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	public static class AgentDependencies
	{
		public static IApmLogger Logger { get; set; }
	}
}
