using System;
using System.Reflection;
using Elastic.Apm.Config;

namespace Elastic.Apm.Tests
{
    public static class TestHelper
    {
        public static void ResetAgentAndEnvVars()
        {
            //reset the static type APM.Agent
            Type staticType = typeof(Apm.Agent);
            ConstructorInfo ci = staticType.TypeInitializer;
            object[] parameters = new object[0];
            ci.Invoke(null, parameters);

            //unset environment variables
            Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, null);
            Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, null);
        }
    }
}
