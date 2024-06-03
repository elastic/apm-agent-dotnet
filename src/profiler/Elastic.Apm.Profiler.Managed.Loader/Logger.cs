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
	internal static class Logger
	{
		static Logger()
		{
			var config = GlobalLogConfiguration.FromEnvironment(Environment.GetEnvironmentVariables());

			Level = config.LogLevel;
			LogFile = config.CreateLogFileName("managed_loader");
			Levels = new Dictionary<LogLevel, string>
			{
				[LogLevel.None] = "OFF  ",
				[LogLevel.Error] = "ERROR",
				[LogLevel.Warning] = "WARN ",
				[LogLevel.Information] = "INFO ",
				[LogLevel.Debug] = "DEBUG",
				[LogLevel.Critical] = "CRITICAL",
				[LogLevel.Trace] = "TRACE",
			};
		}

		private static readonly LogLevel Level;
		private static readonly string LogFile;
		private static readonly Dictionary<LogLevel, string> Levels;

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
