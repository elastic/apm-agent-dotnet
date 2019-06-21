using System;
using System.Runtime.InteropServices;

namespace Elastic.Apm.Helpers
{
	internal static class PlatformDetection
	{
		// Taken from https://github.com/dotnet/corefx/blob/master/src/CoreFx.Private.TestUtilities/src/System/PlatformDetection.cs
		internal static readonly bool IsFullFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
		internal static readonly string DotNetRuntimeDescription = RuntimeInformation.FrameworkDescription;
	}
}
