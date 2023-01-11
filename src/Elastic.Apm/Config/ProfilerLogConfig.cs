// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal struct ProfilerLogConfig
	{
		internal ProfilerLogConfig(LogLevel logLevel, ProfilerLogTarget logTarget, string logFilePath) : this()
		{
			LogLevel = logLevel;
			LogTarget = logTarget;
			LogFilePath = logFilePath;
		}

		internal ProfilerLogTarget LogTarget { get; private set; }
		internal string LogFilePath {  get; private set; }
		internal LogLevel LogLevel {  get; private set; }
		internal static string GetDefaultProfilerLogDirectory()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return Path.Combine(Environment.GetEnvironmentVariable("PROGRAMDATA"), "elastic", "apm-agent-dotnet", "logs");
			return "/var/log/elastic/apm-agent-dotnet";
		}

		internal void TryApplyLogLevel(IApmLogger logger)
		{
			if (logger is ILogLevelSwitchable switchable)
				switchable.LogLevelSwitch.Level = LogLevel;
			else
				logger?.Warning()?.Log($"Logger '{logger}' is not {nameof(ILogLevelSwitchable)}");
		}

		internal static ProfilerLogConfig Check(IApmLogger logger, IDictionary environmentVariables = null)
		{
			environmentVariables ??= new EnvironmentVariables(logger).GetEnvironmentVariables();

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

			var logTarget = ProfilerLogTarget.None;
			foreach (var target in GetSafeEnvironmentVariable("ELASTIC_APM_PROFILER_LOG_TARGETS").Split(';'))
			{
				if (target.Equals("stdout", StringComparison.InvariantCultureIgnoreCase))
					logTarget |= ProfilerLogTarget.StdOut;
				else if (target.Equals("file", StringComparison.InvariantCultureIgnoreCase))
					logTarget |= ProfilerLogTarget.File;
			}
			if (logTarget == ProfilerLogTarget.None)
				logTarget= ProfilerLogTarget.File;

			logger?.Trace()?.Log($"{nameof(ProfilerLogConfig)} - LogLevel: '{logLevel}',  LogTarget: '{logTarget}', LogFileName: '{logFileName}'");

			return new(logLevel, logTarget, logFileName);
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
