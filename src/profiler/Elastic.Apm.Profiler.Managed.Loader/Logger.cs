// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#if PROFILER_MANAGED
namespace Elastic.Apm.Profiler.Managed;
#else
namespace Elastic.Apm.Profiler.Managed.Loader;
#endif

internal static class Logger
{
	static Logger()
	{
		var config = GlobalLogConfiguration.FromEnvironment(Environment.GetEnvironmentVariables());

		Level = config.LogLevel;
		IsActive = config.IsActive;
#if PROFILER_MANAGED
		LogFile = config.CreateLogFileName("profiler_managed");
#else
		LogFile = config.CreateLogFileName("profiler_managed_loader");
#endif
		Levels = new Dictionary<LogLevel, string>
		{
			[LogLevel.None] = "OFF  ",
			[LogLevel.Error] = "ERROR",
			[LogLevel.Warning] = "WARN ",
			[LogLevel.Information] = "INFO ",
			[LogLevel.Debug] = "DEBUG",
			[LogLevel.Critical] = "CRITI",
			[LogLevel.Trace] = "TRACE",
		};
	}

	private static readonly bool IsActive;
	private static readonly LogLevel Level;
	private static readonly string LogFile;
	private static readonly Dictionary<LogLevel, string> Levels;

	public static void Warn(string message, params object[] args) => Log(LogLevel.Warning, message, args);

	public static void Debug(string message, params object[] args) => Log(LogLevel.Debug, message, args);

	public static void Error(Exception exception, string message, params object[] args) => Log(LogLevel.Error, exception, message, args);

	public static void Error(string message, params object[] args) => Log(LogLevel.Error, message, args);

	public static void Log(LogLevel level, Exception exception, string message, params object[] args)
	{
		if (!IsActive || Level > level)
			return;

		Log(level, $"{message}{Environment.NewLine}{exception}", args);
	}

	public static void Log(LogLevel level, string message, params object[] args)
	{
		if (!IsActive || Level > level)
			return;

		if (string.IsNullOrWhiteSpace(LogFile)) return;

		try
		{
			try
			{
				using var stream = File.Open(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
				using var writer = new StreamWriter(stream, new UTF8Encoding(false));
				writer.Write($"[{DateTimeOffset.Now:O}] [{Levels[level]}] ");
				writer.WriteLine(message, args);
				writer.Flush();
				stream.Flush(true);

				return;
			}
			catch
			{
				// ignore
			}

			Console.Error.WriteLine($"[{DateTimeOffset.Now:O}] [{Levels[level]}] {message}", args);
		}
		catch
		{
			// ignore
		}
	}
}
