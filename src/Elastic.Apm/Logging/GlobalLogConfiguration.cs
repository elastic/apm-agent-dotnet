// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#if APM_LOADER
using static Elastic.Apm.Profiler.Managed.Loader.LogEnvironmentVariables;

namespace Elastic.Apm.Profiler.Managed.Loader;
#elif STARTUP_HOOKS
using static ElasticApmStartupHook.LogEnvironmentVariables;

namespace ElasticApmStartupHook;
#else
using static Elastic.Apm.Logging.LogEnvironmentVariables;

namespace Elastic.Apm.Logging;
#endif

internal class EnvironmentLoggingConfiguration(IDictionary environmentVariables = null)
{
	public IDictionary EnvironmentVariables { get; } = environmentVariables ?? Environment.GetEnvironmentVariables();

	public string GetSafeEnvironmentVariable(string key)
	{
		var value = EnvironmentVariables.Contains(key) ? EnvironmentVariables[key]?.ToString() : null;
		return value ?? string.Empty;
	}

	public LogLevel GetLogLevel(params string[] keys)
	{
		var level = keys
			.Select(k => GetSafeEnvironmentVariable(k))
			.Select<string, LogLevel?>(v => v.ToLowerInvariant() switch
			{
				"trace" => LogLevel.Trace,
				"debug" => LogLevel.Debug,
				"info" => LogLevel.Information,
				"warn" => LogLevel.Warning,
				"error" => LogLevel.Error,
				"none" => LogLevel.None,
				_ => null
			})
			.FirstOrDefault(l => l != null);
		return level ?? LogLevel.Warning;
	}

	public string GetLogFilePath(params string[] keys)
	{
		var path = keys
			.Select(k => GetSafeEnvironmentVariable(k))
			.FirstOrDefault(p => !string.IsNullOrEmpty(p));

		return path ?? GetDefaultLogDirectory();
	}

	public bool AnyConfigured(params string[] keys) =>
		keys
			.Select(k => GetSafeEnvironmentVariable(k))
			.Any(p => !string.IsNullOrEmpty(p));

	public GlobalLogTarget ParseLogTargets(params string[] keys)
	{

		var targets = keys
			.Select(k => GetSafeEnvironmentVariable(k))
			.FirstOrDefault(p => !string.IsNullOrEmpty(p));
		if (string.IsNullOrWhiteSpace(targets))
			return GlobalLogTarget.File;

		var logTargets = GlobalLogTarget.None;
		var found = false;

		foreach (var target in targets.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries))
		{
			if (IsSet(target, "stdout"))
				logTargets |= GlobalLogTarget.StdOut;
			else if (IsSet(target, "file"))
				logTargets |= GlobalLogTarget.File;
			else if (IsSet(target, "none"))
				logTargets |= GlobalLogTarget.None;
		}
		return !found ? GlobalLogTarget.File : logTargets;

		bool IsSet(string k, string v)
		{
			var b = k.Trim().Equals(v, StringComparison.InvariantCultureIgnoreCase);
			if (b) found = true;
			return b;
		}
	}

	internal static string GetDefaultLogDirectory() =>
		Environment.OSVersion.Platform == PlatformID.Win32NT
		? Path.Combine(Environment.GetEnvironmentVariable("PROGRAMDATA")!, "elastic", "apm-agent-dotnet", "logs")
		: "/var/log/elastic/apm-agent-dotnet";

}

[Flags]
internal enum GlobalLogTarget
{
	None = 0,
	File = 1,
	StdOut = 2
}

public static class LogEnvironmentVariables
{
	// ReSharper disable once InconsistentNaming
	public const string ELASTIC_OTEL_FILE_LOG_LEVEL = nameof(ELASTIC_OTEL_FILE_LOG_LEVEL);
	public const string ELASTIC_OTEL_FILE_LOG_DIRECTORY = nameof(ELASTIC_OTEL_FILE_LOG_DIRECTORY);
	public const string ELASTIC_OTEL_LOG_TARGETS = nameof(ELASTIC_OTEL_LOG_TARGETS);

	public const string ELASTIC_APM_LOG_LEVEL = nameof(ELASTIC_APM_LOG_LEVEL);
	public const string ELASTIC_APM_LOG_DIRECTORY = nameof(ELASTIC_APM_LOG_DIRECTORY);


	// profiler logs are deprecated in favor of ELASTIC_OTEL_FILE_*
	public const string ELASTIC_APM_PROFILER_LOG = nameof(ELASTIC_APM_PROFILER_LOG);
	public const string ELASTIC_APM_PROFILER_LOG_DIR = nameof(ELASTIC_APM_PROFILER_LOG_DIR);
	public const string ELASTIC_APM_PROFILER_LOG_TARGETS = nameof(ELASTIC_APM_PROFILER_LOG_TARGETS);

	// deprected startup hooks logging configuration, we still listen to it to enable logging
	public const string ELASTIC_APM_STARTUP_HOOKS_LOGGING = nameof(ELASTIC_APM_STARTUP_HOOKS_LOGGING);

	// ReSharper enable once InconsistentNaming
}

internal readonly struct GlobalLogConfiguration
{
	private GlobalLogConfiguration(bool isActive, LogLevel logLevel, GlobalLogTarget logTarget, string logFileDirectory, string logFilePrefix) : this()
	{
		IsActive = isActive;
		LogLevel = logLevel;
		LogTargets = logTarget;
		LogFileDirectory = logFileDirectory;
		LogFilePrefix = logFilePrefix;

		AgentLogFilePath = CreateLogFileName();
	}

	internal bool IsActive { get; }
	internal string AgentLogFilePath { get; }
	internal LogLevel LogLevel { get; }
	internal GlobalLogTarget LogTargets { get; }

	internal string LogFileDirectory { get; }
	internal string LogFilePrefix { get; }

	public override string ToString() => $"IsActive: '{IsActive}', Targets: '{LogTargets}',   Level: '{LogLevel}',  FilePath: '{AgentLogFilePath}'";

	internal static GlobalLogConfiguration FromEnvironment(IDictionary environmentVariables = null)
	{
		var config = new EnvironmentLoggingConfiguration(environmentVariables);
		var logLevel = config.GetLogLevel(ELASTIC_OTEL_FILE_LOG_LEVEL, ELASTIC_APM_PROFILER_LOG, ELASTIC_APM_LOG_LEVEL);
		var logFileDirectory = config.GetLogFilePath(ELASTIC_OTEL_FILE_LOG_DIRECTORY, ELASTIC_APM_PROFILER_LOG_DIR, ELASTIC_APM_LOG_DIRECTORY);
		var logFilePrefix = GetLogFilePrefix();
		var logTarget = config.ParseLogTargets(ELASTIC_OTEL_LOG_TARGETS, ELASTIC_APM_PROFILER_LOG_TARGETS);

		var isActive = config.AnyConfigured(
			ELASTIC_OTEL_FILE_LOG_LEVEL,
			ELASTIC_OTEL_FILE_LOG_DIRECTORY,
			ELASTIC_OTEL_LOG_TARGETS,
			ELASTIC_APM_LOG_DIRECTORY,
			ELASTIC_APM_PROFILER_LOG,
			ELASTIC_APM_PROFILER_LOG_DIR,
			ELASTIC_APM_PROFILER_LOG_TARGETS,
			ELASTIC_APM_STARTUP_HOOKS_LOGGING

		) && logTarget != GlobalLogTarget.None && logLevel != LogLevel.None;

		return new(isActive, logLevel, logTarget, logFileDirectory, logFilePrefix);
	}

	private static string GetLogFilePrefix()
	{
		var process = Process.GetCurrentProcess();
		return $"{process.ProcessName}_{process.Id}_{Environment.TickCount}";
	}

	public string CreateLogFileName(string applicationName = "agent")
	{
		var logFileName = Path.Combine(LogFileDirectory, $"{LogFilePrefix}.{applicationName}.log");
		return logFileName;
	}

}

