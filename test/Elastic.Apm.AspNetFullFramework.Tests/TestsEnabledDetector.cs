// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Tests.Utilities;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal static class TestsEnabledDetector
	{
		private const string EnabledEnvVarName = "ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED";
		private const int MinSupportedIisMajorVersion = 10;

		private const string ReasonPrefix = "ASP.NET Full Framework tests are skipped because";

		private static readonly string ReasonSuffix =
			Environment.NewLine +
			"ASP.NET Full Framework tests are executed when:" + Environment.NewLine +
			"\t1) OS is Windows." + Environment.NewLine +
			"\t2) IIS + ASP.NET are installed with IIS version of at least 10." + Environment.NewLine +
			$"	3) Environment variable {EnabledEnvVarName} is set to true." + Environment.NewLine +
			$"Requirement to set environment variable {EnabledEnvVarName} to true is to have user opt-in. " + Environment.NewLine +
			"Before opting-in please note that these tests perform various operations on this machine's IIS " +
			"such as adding/removing sample applications, stopping/starting application pools, etc." +
			"So it's not recommended to run these tests on a production environment.";

		internal static string ReasonWhyTestsAreSkipped { get; } = DetectReasonWhyTestsAreSkipped();

		private static string DetectReasonWhyTestsAreSkipped()
		{
			if (!TestEnvironment.IsWindows) return $"{ReasonPrefix} OS is not Windows. {ReasonSuffix}";

			if (!CheckIisVersion(out var reason))
				return $"{ReasonPrefix} {reason}. {ReasonSuffix}";

			if (!EnvVarUtils.GetBoolValue(EnabledEnvVarName, /* defaultValue: */ false, out reason))
				return $"{ReasonPrefix} {reason}. {ReasonSuffix}";

			return null;
		}

		private static bool CheckIisVersion(out string reason)
		{
			var w3WpExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"inetsrv\w3wp.exe");
			if (!File.Exists(w3WpExePath))
			{
				reason = $"IIS is not installed (`{w3WpExePath}' not found)";
				return false;
			}

			var versionInfo = FileVersionInfo.GetVersionInfo(w3WpExePath);
			var iisVerMajorPart = versionInfo.ProductMajorPart;
			if (iisVerMajorPart < MinSupportedIisMajorVersion)
			{
				reason = $"Installed IIS version ({iisVerMajorPart}.*) is lower than the minimal supported version ({MinSupportedIisMajorVersion}.*)";
				return false;
			}

			reason = null;
			return true;
		}
	}
}
