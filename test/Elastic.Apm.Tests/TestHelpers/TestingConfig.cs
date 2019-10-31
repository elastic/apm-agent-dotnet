using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class TestingConfig
	{
		internal static class Options
		{
			private const string DefaultConsoleLogLinePrefix = SharedPrefix + "Console> ";
			private const string DefaultSysDiagLogLinePrefix = SharedPrefix;
			private const string NotInIdeDefaultXunitLogLinePrefix = SharedPrefix + "Xunit> ";
			private const string SharedPrefix = "Elastic APM .NET Tests> {0}> ";

			internal static LogLevelOptionMetadata LogLevel = new LogLevelOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_LEVEL", ConsoleLogger.DefaultLogLevel, x => x.LogLevel);

			internal static LogLevelOptionMetadata LogLevelForTestingConfigParsing = new LogLevelOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_LEVEL_FOR_TESTING_CONFIG_PARSING", ConsoleLogger.DefaultLogLevel, x => x.LogLevelForTestingConfigParsing);

			internal static BoolOptionMetadata LogToConsoleEnabled = new BoolOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_CONSOLE_ENABLED", !IsRunningInIde, x => x.LogToConsoleEnabled);

			internal static StringOptionMetadata LogToConsoleLinePrefix = new StringOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_CONSOLE_PREFIX", DefaultConsoleLogLinePrefix, x => x.LogToConsoleLinePrefix);

			internal static BoolOptionMetadata LogToSysDiagTraceEnabled = new BoolOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_SYS_DIAG_TRACE_ENABLED", false, x => x.LogToSysDiagTraceEnabled);

			internal static StringOptionMetadata LogToSysDiagTraceLinePrefix = new StringOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_SYS_DIAG_TRACE_PREFIX", DefaultSysDiagLogLinePrefix, x => x.LogToSysDiagTraceLinePrefix);

			internal static BoolOptionMetadata LogToXunitEnabled = new BoolOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_XUNIT_ENABLED", true, x => x.LogToXunitEnabled);

			internal static StringOptionMetadata LogToXunitLinePrefix = new StringOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_XUNIT_PREFIX", IsRunningInIde ? "" : NotInIdeDefaultXunitLogLinePrefix, x => x.LogToXunitLinePrefix);

			internal static OptionMetadata<int?> RandomSeed = new NullableIntOptionMetadata(
				"ELASTIC_APM_TESTS_RANDOM_SEED", null, x => x.RandomSeed);

			internal static IOptionMetadata[] All =
			{
				LogLevel, LogLevelForTestingConfigParsing, LogToConsoleEnabled, LogToConsoleLinePrefix, LogToSysDiagTraceEnabled,
				LogToSysDiagTraceLinePrefix, LogToXunitEnabled, LogToXunitLinePrefix, RandomSeed
			};


			internal interface IOptionMetadata
			{
				object DefaultValueAsObject { get; }
				PropertyInfo MutableSnapshotPropertyInfo { get; }
				string Name { get; }

				void ParseAndSetProperty(IRawConfigSnapshot rawConfigSnapshot, MutableSnapshot configSnapshot, IApmLogger logger);
			}

			private static LogLevel ParseLogLevel(string valueAsString)
			{
				if (AbstractConfigurationReader.TryParseLogLevel(valueAsString, out var logLevel)) return logLevel;

				throw new FormatException($"`{valueAsString}' is not a valid log level");
			}

			private static string ParseString(string valueAsString) => valueAsString;

			private static Func<string, T?> CreateNullableParser<T>(Func<string, T> nonNullValueParser) where T : struct =>
				valueAsString => valueAsString == null ? (T?)null : nonNullValueParser(valueAsString);

			internal class OptionMetadata<T> : IOptionMetadata
			{
				private readonly Func<string, T> _parser;

				internal OptionMetadata(string optionName, T defaultValue, Func<string, T> parser, Expression<Func<MutableSnapshot, T>> propExpr)
				{
					Name = optionName;
					DefaultValue = defaultValue;
					_parser = parser;
					var expr = (MemberExpression)propExpr.Body;
					MutableSnapshotPropertyInfo = (PropertyInfo)expr.Member;
				}

				internal T DefaultValue { get; }

				public object DefaultValueAsObject => DefaultValue;

				public PropertyInfo MutableSnapshotPropertyInfo { get; }

				public string Name { get; }

				public void ParseAndSetProperty(IRawConfigSnapshot rawConfigSnapshot, MutableSnapshot configSnapshot,
					IApmLogger loggerArg
				)
				{
					IApmLogger logger = loggerArg.Scoped(nameof(IOptionMetadata));
					var parsedValue = ParseValue(logger, rawConfigSnapshot);
					MutableSnapshotPropertyInfo.SetValue(configSnapshot, parsedValue);
				}

				private T ParseValue(IApmLogger logger, IRawConfigSnapshot rawConfigSnapshot)
				{
					var optionValue = rawConfigSnapshot.GetValue(Name);
					if (optionValue == null)
					{
						logger.Trace()
							?.Log("Returning default value because the given raw configuration doesn't map the option name to a value."
								+ $" Raw configuration source: {rawConfigSnapshot.DbgDescription}."
								+ $" Option name: {Name}."
								+ $" Default value: {DefaultValue}.");
						return DefaultValue;
					}

					T parsedValue;
					try
					{
						parsedValue = _parser(optionValue);
					}
					catch (Exception ex)
					{
						logger.Error()
							?.LogException(ex, "Returning default value because parsing of the value returned by the given raw configuration failed."
								+ $" Raw configuration source: {rawConfigSnapshot.DbgDescription}."
								+ $" Option name: {Name}."
								+ $" Value: {optionValue}."
								+ $" Default value: {DefaultValue}.");
						return DefaultValue;
					}

					logger.Trace()
						?.Log("Returning parsed value returned by the given raw configuration."
							+ $" Raw configuration source: {rawConfigSnapshot.DbgDescription}."
							+ $" Option name: {Name}."
							+ $" Value: {optionValue}.");

					return parsedValue;
				}

				public override string ToString() => new ToStringBuilder(nameof(IOptionMetadata))
				{
					{ nameof(Name), Name }, { "Property name", MutableSnapshotPropertyInfo.Name }, { nameof(DefaultValue), DefaultValue }
				}.ToString();
			}

			internal class BoolOptionMetadata : OptionMetadata<bool>
			{
				internal BoolOptionMetadata(string optionName, bool defaultValue, Expression<Func<MutableSnapshot, bool>> propExpr)
					: base(optionName, defaultValue, bool.Parse, propExpr) { }
			}

			internal class LogLevelOptionMetadata : OptionMetadata<LogLevel>
			{
				internal LogLevelOptionMetadata(string optionName, LogLevel defaultValue, Expression<Func<MutableSnapshot, LogLevel>> propExpr)
					: base(optionName, defaultValue, ParseLogLevel, propExpr) { }
			}

			internal class StringOptionMetadata : OptionMetadata<string>
			{
				internal StringOptionMetadata(string optionName, string defaultValue, Expression<Func<MutableSnapshot, string>> propExpr)
					: base(optionName, defaultValue, ParseString, propExpr) { }
			}

			internal class NullableIntOptionMetadata : OptionMetadata<int?>
			{
				internal NullableIntOptionMetadata(string optionName, int? defaultValue, Expression<Func<MutableSnapshot, int?>> propExpr)
					: base(optionName, defaultValue, CreateNullableParser(int.Parse), propExpr) { }
			}
		}

		internal interface IRawConfigSnapshot
		{
			string DbgDescription { get; }

			/// <summary>Gets a configuration value.</summary>
			/// <param name="optionName">The option name.</param>
			/// <returns>The option value or <c>null</c> if the given option name doesn't have a value.</returns>
			string GetValue(string optionName);
		}

		internal interface ISnapshot
		{
			LogLevel LogLevel { get; }

			// ReSharper disable once UnusedMemberInSuper.Global
			LogLevel LogLevelForTestingConfigParsing { get; }

			bool LogToConsoleEnabled { get; }
			string LogToConsoleLinePrefix { get; }

			bool LogToSysDiagTraceEnabled { get; }
			string LogToSysDiagTraceLinePrefix { get; }

			bool LogToXunitEnabled { get; }
			string LogToXunitLinePrefix { get; }

			int? RandomSeed { get; }
		}

		internal static bool IsRunningInIde { get; } = DetectIfRunningInIde();

		internal static ISnapshot ReadFromFromEnvVars(ITestOutputHelper xUnitOutputHelper) =>
			new MutableSnapshot(new EnvVarsRawConfigSnapshot(), xUnitOutputHelper);

		private static bool DetectIfRunningInIde()
		{
			if (Environment.GetEnvironmentVariable("VisualStudioVersion") != null) return true;

			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (string envVarName in Environment.GetEnvironmentVariables().Keys)
			{
				// ReSharper disable once StringLiteralTypo
				if (envVarName.StartsWith("RESHARPER_"))
					return true;
			}

			return false;
		}

		internal class EnvVarsRawConfigSnapshot : IRawConfigSnapshot
		{
			public string DbgDescription => "Environment variables";

			public string GetValue(string optionName) => Environment.GetEnvironmentVariable(optionName);
		}

		internal class MutableSnapshot : ISnapshot
		{
			private const string ThisClassName = nameof(TestingConfig) + "." + nameof(MutableSnapshot);

			internal MutableSnapshot(IRawConfigSnapshot rawConfigSnapshot, ITestOutputHelper xUnitOutputHelper)
			{
				var tempLogger = BuildXunitOutputLogger(ConsoleLogger.DefaultLogLevel);
				Options.LogLevelForTestingConfigParsing.ParseAndSetProperty(rawConfigSnapshot, this, tempLogger);
				var parsingLogger = BuildXunitOutputLogger(LogLevelForTestingConfigParsing);

				foreach (var optionMetadata in Options.All)
				{
					if (optionMetadata == Options.LogLevelForTestingConfigParsing) continue;

					optionMetadata.ParseAndSetProperty(rawConfigSnapshot, this, parsingLogger);
				}

				IApmLogger BuildXunitOutputLogger(LogLevel logLevel)
				{
					return new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(xUnitOutputHelper), logLevel).Scoped(ThisClassName);
				}
			}

			// Used by tests
			internal MutableSnapshot(IRawConfigSnapshot rawConfigSnapshot, IEnumerable<Options.IOptionMetadata> allOptionsMetadata)
			{
				foreach (var optionMetadata in allOptionsMetadata)
					optionMetadata.ParseAndSetProperty(rawConfigSnapshot, this, new NoopLogger());
			}

			public LogLevel LogLevel { get; set; }
			public LogLevel LogLevelForTestingConfigParsing { get; set; }
			public bool LogToConsoleEnabled { get; set; }
			public string LogToConsoleLinePrefix { get; set; }
			public bool LogToSysDiagTraceEnabled { get; set; }
			public string LogToSysDiagTraceLinePrefix { get; set; }
			public bool LogToXunitEnabled { get; set; }
			public string LogToXunitLinePrefix { get; set; }
			public int? RandomSeed { get; set; }
		}
	}
}
