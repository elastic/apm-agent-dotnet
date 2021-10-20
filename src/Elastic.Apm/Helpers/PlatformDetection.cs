// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class PlatformDetection
	{
		internal const string DotNetPrefix = ".NET ";
		internal const string DotNetCoreDescriptionPrefix = ".NET Core";
		internal const string DotNetFullFrameworkDescriptionPrefix = ".NET Framework";
		internal const string MonoDescriptionPrefix = "Mono";

		internal static readonly bool IsDotNetFullFramework =
			// Taken from https://github.com/dotnet/corefx/blob/master/src/CoreFx.Private.TestUtilities/src/System/PlatformDetection.cs#L24
			RuntimeInformation.FrameworkDescription.StartsWith(DotNetFullFrameworkDescriptionPrefix, StringComparison.OrdinalIgnoreCase);

		internal static readonly bool IsDotNetCore =
			// https://github.com/dotnet/corefx/blob/master/src/CoreFx.Private.TestUtilities/src/System/PlatformDetection.cs#L25
			RuntimeInformation.FrameworkDescription.StartsWith(DotNetCoreDescriptionPrefix, StringComparison.OrdinalIgnoreCase);

		internal static readonly bool IsMono =
			RuntimeInformation.FrameworkDescription.StartsWith(MonoDescriptionPrefix, StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Indicates if the runtime is .NET 5 or a newer version.
		/// Specifically looks for ".NET [X]" format.
		/// </summary>
		internal static readonly bool IsDotNet =
			RuntimeInformation.FrameworkDescription.StartsWith(DotNetPrefix, StringComparison.OrdinalIgnoreCase) &&
			RuntimeInformation.FrameworkDescription.Length >= 6
			&& int.TryParse(RuntimeInformation.FrameworkDescription.Substring(5).Split('.')[0], out _);

		internal static readonly string DotNetRuntimeDescription = RuntimeInformation.FrameworkDescription;

		internal static string GetDotNetRuntimeVersionFromDescription(string frameworkDescription, IApmLogger loggerArg, string expectedPrefix)
		{
			var logger = loggerArg.Scoped(nameof(PlatformDetection));
			if (!frameworkDescription.StartsWith(expectedPrefix))
			{
				logger.Trace()
					?.Log("RuntimeInformation.FrameworkDescription (`{DotNetFrameworkRuntimeDescription}') doesn't start" +
						" with the expected prefix (`{DotNetFrameworkRuntimeDescriptionPrefix}')", frameworkDescription, expectedPrefix);
				return null;
			}

			var result = frameworkDescription.Substring(expectedPrefix.Length).Trim();
			logger.Trace()
				?.Log("Version based on RuntimeInformation.FrameworkDescription (`{DotNetFrameworkRuntimeDescription}')" +
					" is `{DotNetFrameworkRuntimeVersion}'", frameworkDescription, result);
			return result;
		}

		internal static Runtime GetServiceRuntime(IApmLogger logger)
		{
			string name;
			if (IsDotNetFullFramework)
				name = Runtime.DotNetFullFrameworkName;
			else if (IsDotNet)
				name = Runtime.DotNetName + $" {RuntimeInformation.FrameworkDescription.Substring(5).Split('.')[0]}";
			else if (IsDotNetCore)
				name = Runtime.DotNetCoreName;
			else if (IsMono)
				name = Runtime.MonoName;
			else
			{
				name = "N/A";
				logger.Error()
					?.Log("Failed to detect whether the current .NET runtime is .NET Full Framework, Mono or .NET Core - " +
						"`{DotNetFrameworkRuntimeName}' will be used as the current .NET runtime name", name);
			}

			string version;
			try
			{
				if (IsDotNetFullFramework)
					version = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
				else if (IsDotNet)
					version = GetNetVersion();
				else if (IsDotNetCore)
					version = GetDotNetCoreRuntimeVersion(logger);
				else if (IsMono)
					version = GetMonoVersion();
				else
				{
					version = "N/A";
					logger.Error()
						?.Log("Failed to detect whether the current .NET runtime is .NET Full Framework or .NET Core - " +
							"`{DotNetFrameworkRuntimeVersion}' will be used as the current .NET runtime version", version);
				}
			}
			catch (Exception ex)
			{
				version = "N/A";
				logger.Error()
					?.LogException(ex, "Exception was thrown while trying to obtain .NET runtime version" +
						" - `{DotNetFrameworkRuntimeVersion}' will be used", version);
			}

			return new Runtime { Name = name, Version = version };
		}

		private static string GetMonoVersion()
			=> RuntimeInformation.FrameworkDescription.Substring(5);

		private static string GetNetVersion()
			=> RuntimeInformation.FrameworkDescription.Substring(5);

		private static string GetDotNetCoreRuntimeVersion(IApmLogger logger)
		{
			// It seems there was no easy way to get .NET Core runtime version until https://github.com/dotnet/corefx/issues/35573 fix.
			// For example see https://github.com/dotnet/BenchmarkDotNet/issues/448 for a contrived way in which BenchmarkDotNet has to do it.
			// But https://github.com/dotnet/corefx/issues/35573 fix is available only from .NET Core 3.0 Preview 4
			// So our implementation should use the above fix if it's available otherwise fallback on BenchmarkDotNet's approach.
			// Unfortunately it's somewhat of "a chicken and a egg" problem because in order to detect if the fix is available we need runtime version
			// but getting runtime version was the initial problem. Fortunately we can detect if the fix is available in a indirect way:
			// before the fix Environment.Version and version in RuntimeInformation.FrameworkDescription were different
			// but after the fix they are almost the same (Environment.Version is only <MAJOR>.<MINOR>.<PATCH>
			// while RuntimeInformation.FrameworkDescription can include version suffix as well). For example:
			//		Environment.Version: 3.0.0
			// 		RuntimeInformation.FrameworkDescription: .NET Core 3.0.0-preview6-27804-01

			var environmentVersion = Environment.Version.ToString();
			var frameworkDescription = RuntimeInformation.FrameworkDescription;
			var frameworkDescriptionVersion =
				GetDotNetRuntimeVersionFromDescription(frameworkDescription, logger, DotNetCoreDescriptionPrefix);
			if (frameworkDescriptionVersion != null)
			{
				if (frameworkDescriptionVersion.StartsWith(environmentVersion))
				{
					// We have https://github.com/dotnet/corefx/issues/35573 fix
					logger.Debug()
						?.Log("Environment.Version (`{DotNetFrameworkRuntimeVersion}')" +
							" and RuntimeInformation.FrameworkDescription (`{DotNetFrameworkRuntimeDescription}')" +
							" refer to the same version (`{DotNetFrameworkRuntimeVersion}') - returning it",
							environmentVersion, frameworkDescription, frameworkDescriptionVersion);
					return frameworkDescriptionVersion;
				}

				logger.Debug()
					?.Log("Environment.Version (`{DotNetFrameworkRuntimeVersion}')" +
						" and version (`{DotNetFrameworkRuntimeVersion}')" +
						" from RuntimeInformation.FrameworkDescription (`{DotNetFrameworkRuntimeDescription}')" +
						" don't refer to the same version", environmentVersion, frameworkDescriptionVersion, frameworkDescription);
			}

			// We don't have https://github.com/dotnet/corefx/issues/35573 fix so we need to fallback on BenchmarkDotNet's approach.
			// We use RichHack approach from https://gist.github.com/richlander/f5849c6967c66d699301f75101906f99/8f73418f7260e207b415ff1a48233cd26094be05
			// (from https://github.com/dotnet/corefx/issues/35361)
			// which essentially the same as BenchmarkDotNet's approach but it's a little bit simpler.
			// Both use filesystem location of System assembly and rely on Microsoft's convention of putting it in
			// "<.NET Core runtime version>" directory. For example on Windows it's usually:
			// C:\Program Files\dotnet\shared\Microsoft.NETCore.App\1.0.14\System.Private.CoreLib.dll
			// C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.2.5\System.Private.CoreLib.dll
			// C:\Program Files\dotnet\shared\Microsoft.NETCore.App\3.0.0-preview6-27804-01\System.Private.CoreLib.dll

			var systemAssemblyFileLocation = typeof(object).Assembly.Location;
			var result = Directory.GetParent(systemAssemblyFileLocation)?.Name;
			logger.Debug()
				?.Log("Falling back on using System assembly file location (`{SystemAssemblyFileLocation}')" +
					" - returning (`{DotNetFrameworkRuntimeVersion}')", systemAssemblyFileLocation, result);
			return result;
		}
	}
}
