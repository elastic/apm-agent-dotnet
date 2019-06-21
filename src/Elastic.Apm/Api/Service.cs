using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Service
	{
		private Service() { }

		public AgentC Agent { get; set; }
		public Framework Framework { get; set; }
		public Language Language { get; set; }
		public string Name { get; set; }
		public Runtime Runtime { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Service))
		{
			{ "Name", Name }, { "Agent", Agent }, { "Framework", Framework }, { "Language", Language },
		}.ToString();

		internal static Service GetDefaultService(IConfigurationReader configurationReader, IApmLogger loggerArg)
		{
			IApmLogger logger = loggerArg.Scoped(nameof(Service));
			return new Service
			{
				Name = configurationReader.ServiceName,
				Agent = new AgentC
				{
					Name = Consts.AgentName,
					Version = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
				},
				Runtime = GetRuntime(logger)
			};
		}

		private static Runtime GetRuntime(IApmLogger logger)
		{
			string name;
			if (PlatformDetection.IsDotNetFullFramework)
				name = Runtime.DotNetFullFrameworkName;
			else if (PlatformDetection.IsDotNetCore)
				name = Runtime.DotNetCoreName;
			else
			{
				name = "N/A";
				logger.Error()
					?.Log($"Failed to detect whether the current .NET runtime is .NET Full Framework or .NET Core - " +
						$"`{name}' will be used as the current .NET runtime name");
			}

			string version;
			try
			{
				if (PlatformDetection.IsDotNetFullFramework)
					version = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
				else if (PlatformDetection.IsDotNetCore)
					version = GetDotNetCoreRuntimeVersion(logger);
				else
				{
					version = "N/A";
					logger.Error()
						?.Log($"Failed to detect whether the current .NET runtime is .NET Full Framework or .NET Core - " +
							$"`{version}' will be used as the current .NET runtime version");
				}
			}
			catch (Exception ex)
			{
				version = "N/A";
				logger.Error()?.LogException(ex, $"Failed to obtain .NET runtime version - `{version}' will be used");
			}

			return new Runtime { Name = name, Version = version };
		}

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
				PlatformDetection.GetDotNetRuntimeVersionFromDescription(frameworkDescription, logger, PlatformDetection.DotNetCoreDescriptionPrefix);
			if (frameworkDescriptionVersion != null)
			{
				if (frameworkDescriptionVersion.StartsWith(environmentVersion))
				{
					// We have https://github.com/dotnet/corefx/issues/35573 fix
					logger.Debug()
						?.Log($"Environment.Version (`{environmentVersion}') and RuntimeInformation.FrameworkDescription (`{frameworkDescription}')" +
							$" refer to the same version (`{frameworkDescriptionVersion}') - returning it");
					return frameworkDescriptionVersion;
				}

				logger.Debug()
					?.Log($"Environment.Version (`{environmentVersion}') and RuntimeInformation.FrameworkDescription (`{frameworkDescription}')" +
						" don't refer to the same version");
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
			var result = Directory.GetParent(systemAssemblyFileLocation).Name;
			logger.Debug()?.Log($"Falling back on using System assembly file location (`{systemAssemblyFileLocation}') - returning (`{result}')");
			return result;
		}

		public class AgentC
		{
			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Name { get; set; }

			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Version { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(AgentC)) { { "Name", Name }, { "Version", Version } }.ToString();
		}
	}

	public class Framework
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Framework)) { { "Name", Name }, { "Version", Version } }.ToString();
	}

	public class Language
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Language)) { { "Name", Name } }.ToString();
	}

	/// <summary>
	/// Name and version of the language runtime running this service
	/// </summary>
	public class Runtime
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Framework)) { { "Name", Name }, { "Version", Version } }.ToString();

		internal const string DotNetFullFrameworkName = ".NET Framework";
		internal const string DotNetCoreName = ".NET Core";
	}
}
