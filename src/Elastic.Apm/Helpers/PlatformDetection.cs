using System;
using System.Runtime.InteropServices;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class PlatformDetection
	{
		internal const string DotNetFullFrameworkDescriptionPrefix = ".NET Framework";
		internal const string DotNetCoreDescriptionPrefix = ".NET Core";

		// Taken from https://github.com/dotnet/corefx/blob/master/src/CoreFx.Private.TestUtilities/src/System/PlatformDetection.cs
		internal static readonly bool IsDotNetFullFramework =
			RuntimeInformation.FrameworkDescription.StartsWith(DotNetFullFrameworkDescriptionPrefix, StringComparison.OrdinalIgnoreCase);

		internal static readonly bool IsDotNetCore =
			RuntimeInformation.FrameworkDescription.StartsWith(DotNetCoreDescriptionPrefix, StringComparison.OrdinalIgnoreCase);

		internal static readonly string DotNetRuntimeDescription = RuntimeInformation.FrameworkDescription;

		internal static string GetDotNetRuntimeVersionFromDescription(string frameworkDescription, IApmLogger loggerArg, string expectedPrefix)
		{
			var logger = loggerArg.Scoped(nameof(PlatformDetection));
			if (!frameworkDescription.StartsWith(expectedPrefix))
			{
				logger.Trace()
					?.Log($"RuntimeInformation.FrameworkDescription (`{frameworkDescription}') doesn't start" +
						$" with the expected prefix (`{expectedPrefix}')");
				return null;
			}

			var result = frameworkDescription.Substring(expectedPrefix.Length).Trim();
			logger.Trace()?.Log($"Version based on RuntimeInformation.FrameworkDescription (`{frameworkDescription}') is `{result}'");
			return result;
		}

	}
}
