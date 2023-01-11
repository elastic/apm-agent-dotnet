// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal readonly struct ProfilerLogConfig
	{
		private ProfilerLogConfig(LogLevel logLevel, ProfilerLogTarget logTarget, string logFilePath) : this()
		{
			LogLevel = logLevel;
			LogTargets = logTarget;
			LogFilePath = logFilePath;
		}

		internal ProfilerLogTarget LogTargets { get; }
		internal string LogFilePath { get; }
		internal LogLevel LogLevel { get; }

		public override string ToString() =>
			$"LogLevel: '{LogLevel}',  LogTargets: '{LogTargets}', LogFilePath: '{LogFilePath}'";

		internal static string GetDefaultProfilerLogDirectory() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? Path.Combine(Environment.GetEnvironmentVariable("PROGRAMDATA"), "elastic", "apm-agent-dotnet", "logs")
			: "/var/log/elastic/apm-agent-dotnet";

		internal static ProfilerLogConfig Check(IDictionary environmentVariables = null)
		{
			environmentVariables ??= Environment.GetEnvironmentVariables();

			string GetSafeEnvironmentVariable(string key)
			{
				var value = environmentVariables.Contains(key) ? environmentVariables[key]?.ToString() : null;
				return value ?? string.Empty;
			}

			var logLevel = GetSafeEnvironmentVariable("ELASTIC_APM_PROFILER_LOG").ToLowerInvariant() switch
			{
				"trace" => LogLevel.Trace,
				"debug" => LogLevel.Debug,
				"info" => LogLevel.Information,
				"warn" => LogLevel.Warning,
				"error" => LogLevel.Error,
				_ => LogLevel.None,
			};

			var logFilePath = GetSafeEnvironmentVariable("ELASTIC_APM_PROFILER_LOG_DIR");
			if (string.IsNullOrEmpty(logFilePath))
				logFilePath = GetDefaultProfilerLogDirectory();
			var process = Process.GetCurrentProcess();
			var logFileName = Path.Combine(logFilePath, $"{process.ProcessName}_{process.Id}_{Environment.TickCount}.agent.log");

			var logTargets = ProfilerLogTarget.None;
			foreach (var target in GetSafeEnvironmentVariable("ELASTIC_APM_PROFILER_LOG_TARGETS").Split(';'))
			{
				if (target.Equals("stdout", StringComparison.InvariantCultureIgnoreCase))
					logTargets |= ProfilerLogTarget.StdOut;
				else if (target.Equals("file", StringComparison.InvariantCultureIgnoreCase))
					logTargets |= ProfilerLogTarget.File;
			}

			if (logTargets == ProfilerLogTarget.None)
				logTargets = ProfilerLogTarget.File;

			return new(logLevel, logTargets, logFileName);
		}
	}

	[Flags]
	internal enum ProfilerLogTarget
	{
		None = 0,
		File = 1,
		StdOut = 2
	}
}
