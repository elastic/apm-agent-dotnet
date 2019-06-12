using System;
using System.Runtime.InteropServices;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal static class TestsEnabledDetector
	{
		private const string EnabledEnvVarName = "ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED";

		private const string ReasonPrefix = "ASP.NET Full Framework tests are skipped because";
		internal static string ReasonWhyTestsAreSkipped { get; } = DetectReasonWhyTestsAreSkipped();

		private static string DetectReasonWhyTestsAreSkipped()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"{ReasonPrefix} OS is not Windows";

			if (!GetEnabledEnvVarValue(out var reason))
				return $"{ReasonPrefix} {reason}";

			return null;
		}

		private static bool GetEnabledEnvVarValue(out string reason)
		{
			var envVarValue = Environment.GetEnvironmentVariable(EnabledEnvVarName);
			if (envVarValue == null)
			{
				reason = $"environment variable `{EnabledEnvVarName}' is not set";
				return false;
			}

			reason = $"environment variable `{EnabledEnvVarName}' is set to `{envVarValue}'";
			if (!bool.TryParse(envVarValue, out var envVarBoolValue))
			{
				reason += " which is not a valid boolean value";
				return false;
			}

			return envVarBoolValue;
		}
	}
}
