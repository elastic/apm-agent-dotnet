using System;
using Elastic.Apm.Config;

namespace Elastic.Apm.Tests
{
	public static class TestHelper
	{
		public static void ResetAgentAndEnvVars()
		{
			//reset the static type APM.Agent
			var staticType = typeof(Agent);
			var ci = staticType.TypeInitializer;
			var parameters = new object[0];
			ci.Invoke(null, parameters);

			//unset environment variables
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, null);
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, null);
		}
	}
}
