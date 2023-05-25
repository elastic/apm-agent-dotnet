// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Elastic.Apm.Profiler.Managed.Loader
{
	// match the log levels of the profiler logger
	internal enum LogLevel
	{
		Trace = 0,
		Debug = 1,
		Info = 2,
		Warn = 3,
		Error = 4,
		Off = 5,
	}

	internal static class Logger
	{
		static Logger()
		{
			Level = GetLogLevel(LogLevel.Warn);
			var logDirectory = GetLogDirectory();
			LogFile = GetLogFile(logDirectory);
			Levels = new Dictionary<LogLevel, string>
			{
				[LogLevel.Off] = "OFF  ",
				[LogLevel.Error] = "ERROR",
				[LogLevel.Warn] = "WARN ",
				[LogLevel.Info] = "INFO ",
				[LogLevel.Debug] = "DEBUG",
				[LogLevel.Trace] = "TRACE",
			};
		}

		private static readonly LogLevel Level;
		private static readonly string LogFile;
		private static readonly Dictionary<LogLevel,string> Levels;

		public static void Log(LogLevel level, Exception exception, string message, params object[] args)
		{
			if (Level > level)
				return;

			Log(level, $"{message}{Environment.NewLine}{exception}", args);
		}

		public static void Log(LogLevel level, string message, params object[] args)
		{
			if (Level > level)
				return;

			try
			{
				if (LogFile != null)
				{
					try
					{
						using (var stream = File.Open(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read))
						using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
						{
							writer.Write($"[{DateTimeOffset.Now:O}] [{Levels[level]}] ");
							writer.WriteLine(message, args);
							writer.Flush();
							stream.Flush(true);
						}

						return;
					}
					catch
					{
						// ignore
					}
				}

				Console.Error.WriteLine($"[{DateTimeOffset.Now:O}] [{Levels[level]}] {message}", args);
			}
			catch
			{
				// ignore
			}
		}

		private static string GetLogFile(string logDirectory)
		{
			if (logDirectory is null)
				return null;

			var process = Process.GetCurrentProcess();
			return Path.Combine(logDirectory, $"Elastic.Apm.Profiler.Managed.Loader_{process.ProcessName}_{process.Id}.log");
		}

		private static LogLevel GetLogLevel(LogLevel defaultLevel)
		{
			var level = Environment.GetEnvironmentVariable("ELASTIC_APM_PROFILER_LOG");
			if (string.IsNullOrEmpty(level))
				return defaultLevel;

			return Enum.TryParse<LogLevel>(level, true, out var parsedLevel)
				? parsedLevel
				: defaultLevel;
		}

		private static string GetLogDirectory()
		{
			try
			{
				var logDirectory = Environment.GetEnvironmentVariable("ELASTIC_APM_PROFILER_LOG_DIR");
				if (string.IsNullOrEmpty(logDirectory))
				{
					if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					{
						var programData = Environment.GetEnvironmentVariable("PROGRAMDATA");
						logDirectory = !string.IsNullOrEmpty(programData)
							? Path.Combine(programData, "elastic", "apm-agent-dotnet", "logs")
							: ".";
					}
					else
						logDirectory = "/var/log/elastic/apm-agent-dotnet";
				}

				Directory.CreateDirectory(logDirectory);
				return logDirectory;
			}
			catch
			{
				return null;
			}
		}
	}
}
