// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigurationOption;

namespace Elastic.Apm.Config
{
	internal class OptionLoggingInstruction
	{
		internal OptionLoggingInstruction(ConfigurationOption option)
		{
			Option = option;
			LogAlways = Option switch
			{
				ServerUrl => true,
				ServiceName => true,
				ServiceVersion => true,
				ConfigurationOption.LogLevel => true,
				_ => false
			};
		}

		public ConfigurationOption Option { get; }
		/// <summary>
		/// Forces evaluation of the default value rather then logging the un-configured value
		/// </summary>
		public bool LogAlways { get; }

		public override string ToString() => $"{Option.ToNormalizedName()}";
	}

	internal static class ConfigurationLogger
	{
		internal static DefaultKeyValue GetDefaultValueForLogging(ConfigurationOption option, IConfigurationReader config) =>
			option switch
			{
				ServerUrl => new(option, config.ServerUrl.AbsoluteUri, nameof(GetDefaultValueForLogging)),
				ServiceName => new(option, config.ServiceName, nameof(GetDefaultValueForLogging)),
				ServiceVersion => new(option, config.ServiceVersion, nameof(GetDefaultValueForLogging)),
				ConfigurationOption.LogLevel => new(option, config.LogLevel.ToString(), nameof(GetDefaultValueForLogging)),
				_ => null,
			};

		internal static IReadOnlyCollection<OptionLoggingInstruction> OptionLoggingInstructions { get; } =
			ConfigurationOptionExtensions.AllOptions()
				.Select(o => new OptionLoggingInstruction(o))
				.OrderByDescending(i => i.LogAlways ? 1 : 0)
				.ToArray();

		public static void PrintAgentLogPreamble(IApmLogger logger, IConfigurationReader configurationReader)
		{
			if (logger?.Info() == null)
				return;

			try
			{
				var info = logger.Info()!.Value;
				var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
				info.Log("********************************************************************************");
				info.Log("Elastic APM .NET Agent, version: {ApmAgentVersion}", version);
				info.Log("Process ID: {ProcessId}", Process.GetCurrentProcess().Id);
				info.Log("Process Name: {ProcessName}", Process.GetCurrentProcess().ProcessName);
				info.Log("Command line arguments: '{ProcessArguments}'", string.Join(", ", System.Environment.GetCommandLineArgs()));
				info.Log("Operating System: {OSDescription}", RuntimeInformation.OSDescription);
				info.Log("CPU architecture: {OSArchitecture}", RuntimeInformation.OSArchitecture);
				info.Log("Host: {HostName}", System.Environment.MachineName);
				info.Log("Time zone: {TimeZone}", TimeZoneInfo.Local);
				info.Log("Runtime: {RunTime}", RuntimeInformation.FrameworkDescription);

#if NET9_0
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "net9.0");
#elif NET8_0
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "net8.0");
#elif NET7_0
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "net7.0");
#elif NET6_0
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "net6.0");
#elif NET5_0
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "net5.0");
#elif NETSTANDARD2_0
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "netstandard2.0");
#elif NETSTANDARD2_1
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "netstandard2.1");
#elif NET462
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "net462");
#else
				info.Log("Matched TFM: {TargetFrameworkMoniker}", "<unknown>");
#endif

				PrintAgentConfiguration(logger, configurationReader);
			}
			catch (Exception e)
			{
				logger.Warning()?.LogException(e, $"Unexpected exception during {nameof(PrintAgentLogPreamble)}");
			}
		}

		public static void PrintAgentConfiguration(IApmLogger logger, IConfigurationReader configurationReader)
		{
			if (logger?.Info() == null)
				return;
			try
			{
				var info = logger.Info()!.Value;
				info.Log("********************************************************************************");
				info.Log("Agent Configuration (via '{ConfigurationProvider}'):"
					, configurationReader.Description ?? configurationReader.GetType().ToString());

				var activeConfiguration = OptionLoggingInstructions
					.Select(instruction =>
					{
						var configuration = configurationReader.Lookup(instruction.Option);
						if (configuration == null || (string.IsNullOrEmpty(configuration.Value) && instruction.LogAlways))
							configuration = GetDefaultValueForLogging(instruction.Option, configurationReader);
						return (instruction, configuration);
					})
					.Where(t => t.configuration != null)
					.Where(t => t.instruction.LogAlways || (!t.instruction.LogAlways && !string.IsNullOrWhiteSpace(t.configuration.Value)))
					.OrderBy(t => t.instruction.LogAlways ? 0 : 1)
					.ThenBy(t => t.configuration.Option.ToNormalizedName())
					.ThenBy(t => string.IsNullOrEmpty(t.configuration.Value) ? 1 : 0);

				foreach (var (_, configuration) in activeConfiguration)
				{
					configuration.Log(info, static (l, type, name, value, origin) =>
						l.Log("{Type}->{Name}: '{Value}' ({Origin})", type, name, value, origin));
				}

				info.Log("********************************************************************************");
			}
			catch (Exception e)
			{
				logger.Warning()?.LogException(e, $"Unexpected exception during {nameof(PrintAgentConfiguration)}");
			}
		}
	}
}
