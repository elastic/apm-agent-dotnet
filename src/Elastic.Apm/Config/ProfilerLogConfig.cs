// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config;

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

	public string CreateLogFileName(string logFilePath)
	{
		var process = Process.GetCurrentProcess();
		var logFileName = Path.Combine(logFilePath, $"{process.ProcessName}_{process.Id}_{Environment.TickCount}.agent.log");
		return logFileName;
	}

	public GlobalLogTarget ParseLogTargets(params string[] keys)
	{

		var targets = keys
			.Select(k => GetSafeEnvironmentVariable(k))
			.FirstOrDefault(p => !string.IsNullOrEmpty(p));
		if (string.IsNullOrWhiteSpace(targets))
			return GlobalLogTarget.File;

		var logTargets = GlobalLogTarget.None;
		foreach (var target in targets.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries))
		{
			if (target.Trim().Equals("stdout", StringComparison.InvariantCultureIgnoreCase))
				logTargets |= GlobalLogTarget.StdOut;
			else if (target.Trim().Equals("file", StringComparison.InvariantCultureIgnoreCase))
				logTargets |= GlobalLogTarget.File;
		}
		if (logTargets == GlobalLogTarget.None)
			logTargets = GlobalLogTarget.File;
		return logTargets;

	}

	internal static string GetDefaultLogDirectory() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
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

internal readonly struct GlobalLogConfiguration
{
	private GlobalLogConfiguration(bool isActive, LogLevel logLevel, GlobalLogTarget logTarget, string logFilePath) : this()
	{
		IsActive = isActive;
		LogLevel = logLevel;
		LogTargets = logTarget;
		LogFilePath = logFilePath;
	}

	internal bool IsActive { get; }
	internal string LogFilePath { get; }
	internal LogLevel LogLevel { get; }
	internal GlobalLogTarget LogTargets { get; }

	public override string ToString() => $"LogLevel: '{LogLevel}',  LogFilePath: '{LogFilePath}'";

	internal static GlobalLogConfiguration FromEnvironment(IDictionary environmentVariables = null)
	{
		var config = new EnvironmentLoggingConfiguration(environmentVariables);
		var logLevel = config.GetLogLevel("ELASTIC_OTEL_FILE_LOG_LEVEL", "ELASTIC_APM_LOG_LEVEL", "ELASTIC_APM_PROFILER_LOG");
		var logFileDirectory = config.GetLogFilePath("ELASTIC_OTEL_FILE_LOG_DIRECTORY", "ELASTIC_APM_LOG_DIRECTORY", "ELASTIC_APM_PROFILER_LOG_DIR");
		var logFilePath = config.CreateLogFileName(logFileDirectory);
		var logTarget = config.ParseLogTargets("ELASTIC_APM_PROFILER_LOG_TARGETS");

		var isActive = config.AnyConfigured(
			"ELASTIC_OTEL_FILE_LOG_LEVEL",
			"ELASTIC_OTEL_FILE_LOG_DIRECTORY",
			"ELASTIC_APM_LOG_DIRECTORY",
			"ELASTIC_APM_PROFILER_LOG",
			"ELASTIC_APM_PROFILER_LOG_DIR",
			"ELASTIC_APM_PROFILER_LOG_TARGETS"
		) && logTarget != GlobalLogTarget.None;

		return new(isActive, logLevel, logTarget, logFilePath);
	}
}

