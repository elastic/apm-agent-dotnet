using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Elastic.Apm.AspNetFullFramework;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace AspNetFullFrameworkSampleApp
{
	public class LoggingConfig
	{
		public const string LogFileEnvVarName = "ELASTIC_APM_ASP_NET_FULL_FRAMEWORK_SAMPLE_APP_LOG_FILE";

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static readonly MemoryTarget LogMemoryTarget = new MemoryTarget();

		public static void SetupLogging()
		{
			var logFileEnvVarValue = Environment.GetEnvironmentVariable(LogFileEnvVarName);

			var config = new LoggingConfiguration();
			const string layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss.fff zzz}" +
				" | ${level:uppercase=true:padding=-5}" + // negative values cause right padding
				" | ${threadname:padding=-30:whenEmpty=${threadid:padding=-30}}" +
				"${when:when=length('${logger}') > 0:inner= | ${logger}}" +
				" | ${message}" +
				"${onexception:${newline}+-> Exception\\: ${exception:format=ToString}";

			var logTargets = new List<TargetWithLayout>
			{
				new PrefixingTraceTarget($"Elastic APM .NET {nameof(AspNetFullFrameworkSampleApp)}> "), LogMemoryTarget, new ConsoleTarget()
			};

			if (logFileEnvVarValue != null) logTargets.Add(new FileTarget { FileName = logFileEnvVarValue, DeleteOldFileOnStartup = true });

			foreach (var logTarget in logTargets) logTarget.Layout = layout;

			// ReSharper disable once CoVariantArrayConversion
			config.AddRule(LogLevel.Trace, LogLevel.Fatal, new SplitGroupTarget(logTargets.ToArray()));

			InternalLogger.LogToConsole = true;
			if (logFileEnvVarValue != null) InternalLogger.LogFile = logFileEnvVarValue;
			InternalLogger.LogLevel = LogLevel.Info;
			InternalLogger.LogWriter = new StringWriter();

			// Apply NLog config
			LogManager.Configuration = config;

			// Set up Elastic APM .NET Agent to use NLog for its logging
			AgentDependencies.Logger = new ApmLoggerToNLog();

			Logger.Debug(nameof(SetupLogging) + " completed. Path to log file: {SampleAppLogFilePath}", logFileEnvVarValue);
		}

		private sealed class PrefixingTraceTarget : TargetWithLayout
		{
			// The order in endOfLines is important because we need to check longer sequences first
			private static readonly string[] EndOfLineCharSequences = { "\r\n", "\n", "\r" };
			private readonly string _prefix;

			internal PrefixingTraceTarget(string prefix = "")
			{
				_prefix = prefix;
				OptimizeBufferReuse = true;
			}

			protected override void Write(LogEventInfo logEvent)
			{
				var message = RenderLogEvent(Layout, logEvent);
				Trace.WriteLine(PrefixEveryLine(message, _prefix));
			}

			private static string PrefixEveryLine(string input, string prefix = "")
			{
				// We treat empty input as a special case because StringReader doesn't return it as an empty line
				if (input.Length == 0) return prefix;

				var resultBuilder = new StringBuilder(input.Length);
				using (var stringReader = new StringReader(input))
				{
					var isFirstLine = true;
					string line;
					while ((line = stringReader.ReadLine()) != null)
					{
						if (isFirstLine)
							isFirstLine = false;
						else
							resultBuilder.AppendLine();
						resultBuilder.Append(prefix);
						resultBuilder.Append(line);
					}
				}

				// Since lines returned by StringReader exclude newline characters it's possible that the last line had newline at the end
				// but we didn't append it

				foreach (var endOfLineSeq in EndOfLineCharSequences)
				{
					if (!input.EndsWith(endOfLineSeq)) continue;

					resultBuilder.Append(endOfLineSeq);
					break;
				}

				return resultBuilder.ToString();
			}
		}
	}
}
