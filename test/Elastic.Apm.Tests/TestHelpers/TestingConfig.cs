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
			private const string DefaultLogLinePrefix = "Elastic APM .NET Tests> {0}> ";

			internal static LogLevelOptionMetadata LogLevel = new LogLevelOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_LEVEL", ConsoleLogger.DefaultLogLevel, x => x.LogLevel);

			internal static LogLevelOptionMetadata LogLevelForTestingConfigParsing = new LogLevelOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_LEVEL_FOR_TESTING_CONFIG_PARSING", ConsoleLogger.DefaultLogLevel, x => x.LogLevelForTestingConfigParsing);

			internal static BoolOptionMetadata LogToConsoleEnabled = new BoolOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_CONSOLE_ENABLED", false, x => x.LogToConsoleEnabled);

			internal static BoolOptionMetadata LogToSysDiagTraceEnabled = new BoolOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_SYS_DIAG_TRACE_ENABLED", false, x => x.LogToSysDiagTraceEnabled);

			internal static StringOptionMetadata LogToSysDiagTraceLinePrefix = new StringOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_SYS_DIAG_TRACE_PREFIX", DefaultLogLinePrefix, x => x.LogToSysDiagTraceLinePrefix);

			internal static StringOptionMetadata LogToConsoleLinePrefix = new StringOptionMetadata(
				"ELASTIC_APM_TESTS_LOG_CONSOLE_PREFIX", DefaultLogLinePrefix, x => x.LogToConsoleLinePrefix);

			internal static OptionMetadata<int?> RandomSeed = new NullableIntOptionMetadata(
				"ELASTIC_APM_TESTS_RANDOM_SEED", null, x => x.RandomSeed);

			internal static OptionMetadata[] All =
			{
				LogLevelForTestingConfigParsing, LogLevel, LogToConsoleEnabled, LogToSysDiagTraceEnabled, LogToSysDiagTraceLinePrefix,
				LogToConsoleLinePrefix, RandomSeed
			};


			internal interface OptionMetadata
			{
				object DefaultValueAsObject { get; }
				PropertyInfo MutableSnapshotPropertyInfo { get; }
				string Name { get; }

				void ParseAndSetProperty(RawConfigSnapshot rawConfigSnapshot, MutableSnapshot configSnapshot, IApmLogger logger);
			}

			private static LogLevel ParseLogLevel(string valueAsString)
			{
				if (AbstractConfigurationReader.TryParseLogLevel(valueAsString, out var logLevel)) return logLevel;

				throw new FormatException($"`{valueAsString}' is not a valid log level");
			}

			private static string ParseString(string valueAsString) => valueAsString;

			private static Func<string, T?> CreateNullableParser<T>(Func<string, T> nonNullValueParser) where T : struct =>
				valueAsString => valueAsString == null ? (T?)null : nonNullValueParser(valueAsString);

			internal class OptionMetadata<T> : OptionMetadata
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

				public void ParseAndSetProperty(RawConfigSnapshot rawConfigSnapshot, MutableSnapshot configSnapshot,
					IApmLogger loggerArg
				)
				{
					IApmLogger logger = loggerArg.Scoped(nameof(OptionMetadata));
					var parsedValue = ParseValue(logger, rawConfigSnapshot);
					MutableSnapshotPropertyInfo.SetValue(configSnapshot, parsedValue);
				}

				private T ParseValue(IApmLogger logger, RawConfigSnapshot rawConfigSnapshot)
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

				public override string ToString() => new ToStringBuilder(nameof(OptionMetadata))
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

		internal interface RawConfigSnapshot
		{
			string DbgDescription { get; }

			/// <summary>Gets a configuration value.</summary>
			/// <param name="key">The option name.</param>
			/// <returns>The option value or <c>null</c> if the given option name doesn't have a value.</returns>
			string GetValue(string optionName);
		}

		internal interface Snapshot
		{
			LogLevel LogLevel { get; }
			LogLevel LogLevelForTestingConfigParsing { get; }
			bool LogToConsoleEnabled { get; }
			string LogToConsoleLinePrefix { get; }
			bool LogToSysDiagTraceEnabled { get; }
			string LogToSysDiagTraceLinePrefix { get; }
			int? RandomSeed { get; }
		}

		internal static Snapshot ReadFromFromEnvVars(ITestOutputHelper xUnitOutputHelper) =>
			new MutableSnapshot(new EnvVarsRawConfigSnapshot(), xUnitOutputHelper);

		internal class EnvVarsRawConfigSnapshot : RawConfigSnapshot
		{
			public string DbgDescription => "Environment variables";

			public string GetValue(string optionName) => Environment.GetEnvironmentVariable(optionName);
		}

		internal class MutableSnapshot : Snapshot
		{
			internal MutableSnapshot(RawConfigSnapshot rawConfigSnapshot, ITestOutputHelper xUnitOutputHelper)
			{
				var tempLogger = new XunitOutputLogger(xUnitOutputHelper, ConsoleLogger.DefaultLogLevel).Scoped(nameof(MutableSnapshot));
				Options.LogLevelForTestingConfigParsing.ParseAndSetProperty(rawConfigSnapshot, this, tempLogger);
				var parsingLogger = new XunitOutputLogger(xUnitOutputHelper, LogLevelForTestingConfigParsing).Scoped(nameof(MutableSnapshot));

				foreach (var optionMetadata in Options.All)
				{
					if (optionMetadata == Options.LogLevelForTestingConfigParsing) continue;

					optionMetadata.ParseAndSetProperty(rawConfigSnapshot, this, parsingLogger);
				}
			}

			// Used by tests
			internal MutableSnapshot(RawConfigSnapshot rawConfigSnapshot, IEnumerable<Options.OptionMetadata> allOptionsMetadata)
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
			public int? RandomSeed { get; set; }
		}
	}
}
