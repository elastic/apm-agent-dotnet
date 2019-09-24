using System;
using System.Collections.Generic;
using System.IO;
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
				new TraceTarget(),
				LogMemoryTarget,
				new ConsoleTarget()
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
	}
}
