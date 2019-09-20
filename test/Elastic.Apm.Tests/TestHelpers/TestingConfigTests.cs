using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Tests.TestHelpers.TestingConfig;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class TestingConfigTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(TestingConfigTests);

		private static readonly bool[] PossibleBoolDefaultValues = { false, true };

		private static readonly LogLevel[] PossibleLogLevelDefaultValues = (LogLevel[])Enum.GetValues(typeof(LogLevel));

		private static readonly string[] PossibleStringDefaultValues =
		{
			null, "", "1", "22", "123", "some default value", "with various \t white \n\t space"
		};

		private static readonly ValueTuple<string, object>[] PossibleStringToBoolVariants =
		{
			("false", (object)false), ("true", true), ("FALSE", false), ("tRUe", true), ("", null), ("qwerty", null)
		};

		private static readonly IEnumerable<ValueTuple<string, object>> PossibleStringToLogLevelVariants = GetPossibleStringToLogLevelVariants();
		private static readonly int?[] PossibleNullableIntDefaultValues = { 0, 1, 22, 123, null };


		private static readonly TimeSpan[] PossibleTimeSpanDefaultValues =
		{
			TimeSpan.Zero, 100.Milliseconds(), 1.Second(), 30.Seconds(), 1.Minute(), 5.Minutes(), 2.Hours()
		};

		private static readonly AbstractConfigurationReader.TimeSuffix[] PossibleTimeSuffixValues =
			(AbstractConfigurationReader.TimeSuffix[])Enum.GetValues(typeof(AbstractConfigurationReader.TimeSuffix));

		private static readonly IEnumerable<ValueTuple<string, object>>
			PossibleStringToNullableIntVariants = GetPossibleStringToNullableIntVariants();


		private readonly IApmLogger _logger;

		public TestingConfigTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) => _logger = LoggerBase.Scoped(ThisClassName);

		private static IEnumerable<ValueTuple<string, object>> PossibleStringToStringVariants =>
			PossibleStringDefaultValues.Select(defaultValue => (defaultValue, (object)defaultValue));

		private static IEnumerable<ValueTuple<string, object>> GetPossibleStringToLogLevelVariants()
		{
			var counter = 0;
			foreach (var logLevel in PossibleLogLevelDefaultValues)
			{
				var defaultValueAsString = logLevel.ToString();

				yield return (defaultValueAsString, (object)logLevel);
				yield return (defaultValueAsString.ToLower(), (object)logLevel);
				yield return (defaultValueAsString.ToUpper(), (object)logLevel);
				yield return (UppercaseLetterAt(defaultValueAsString, counter % defaultValueAsString.Length), (object)logLevel);

				yield return (defaultValueAsString.Remove(counter % defaultValueAsString.Length), (object)null);

				++counter;
			}

			var invalidLogLevelStrings = new[] { "", "x", "some text", "with various \t white \n\t space" };
			foreach (var invalidLogLevelString in invalidLogLevelStrings) yield return (invalidLogLevelString, (object)null);

			string UppercaseLetterAt(string str, int index)
			{
				return (index > 0 ? str.Substring(0, index) : "")
					+ char.ToUpper(str[index], CultureInfo.InvariantCulture)
					+ (index < str.Length - 1 ? str.Substring(index + 1) : "");
			}
		}

		private static IEnumerable<ValueTuple<string, object>> GetPossibleStringToNullableIntVariants()
		{
			foreach (var defaultValue in PossibleNullableIntDefaultValues)
			{
				if (defaultValue == null) continue;

				yield return (defaultValue.ToString(), (object)defaultValue);
			}

			yield return ("", (object)null);
			yield return ("a", (object)null);
			yield return ("xyz", (object)null);
			yield return ("1a", (object)null);
			yield return ("z9", (object)null);
			yield return ("qwerty", (object)null);
		}

		private static IEnumerable<ValueTuple<string, TimeSpan?>> GetPossibleStringToTimeSpanVariants(
			AbstractConfigurationReader.TimeSuffix defaultTimeSuffix
		)
		{
			var counter = 0L;
			foreach (var (timeSpanAsString, timeSpan) in UpperLowerVariations(SuffixVariations(ValidValues().Concat(InvalidValues()))))
			{
				++counter;
				yield return (timeSpanAsString, timeSpan);
			}

			counter.Should().BeGreaterThan(PossibleTimeSpanDefaultValues.Length);

			IEnumerable<ValueTuple<string, TimeSpan?>> UpperLowerVariations(IEnumerable<ValueTuple<string, TimeSpan?>> source)
			{
				foreach (var (timeSpanAsString, timeSpan) in source)
				{
					yield return (timeSpanAsString, timeSpan);

					if (!timeSpanAsString.ToUpper().Equals(timeSpanAsString, StringComparison.Ordinal))
						yield return (timeSpanAsString.ToUpper(), timeSpan);

					if (!timeSpanAsString.ToLower().Equals(timeSpanAsString, StringComparison.Ordinal))
						yield return (timeSpanAsString.ToLower(), timeSpan);
				}
			}

			IEnumerable<ValueTuple<string, TimeSpan?>> SuffixVariations(IEnumerable<ValueTuple<string, TimeSpan?>> source)
			{
				foreach (var (timeSpanAsString, timeSpan) in source)
				{
					yield return (timeSpanAsString, timeSpan);
					yield return (timeSpanAsString + "" + defaultTimeSuffix, timeSpan);
					yield return (timeSpanAsString + " " + defaultTimeSuffix, timeSpan);
					yield return (timeSpanAsString + " \t " + defaultTimeSuffix, timeSpan);
					yield return (timeSpanAsString + "-" + defaultTimeSuffix, null);
					yield return (timeSpanAsString + "_" + defaultTimeSuffix, null);
				}
			}

			IEnumerable<ValueTuple<string, TimeSpan?>> ValidValues()
			{
				foreach (var timeSpan in PossibleTimeSpanDefaultValues)
					yield return (GetValueForSuffix(timeSpan, defaultTimeSuffix).ToString(CultureInfo.InvariantCulture), timeSpan);
			}

			double GetValueForSuffix(TimeSpan timeSpan, AbstractConfigurationReader.TimeSuffix timeSuffix)
			{
				switch (timeSuffix)
				{
					case AbstractConfigurationReader.TimeSuffix.M: return timeSpan.TotalMinutes;
					case AbstractConfigurationReader.TimeSuffix.Ms: return timeSpan.TotalMilliseconds;
					case AbstractConfigurationReader.TimeSuffix.S: return timeSpan.TotalSeconds;
					default:
						throw new AssertionFailedException($"Unexpected TimeSuffix value: {timeSuffix} (as int: {(int)timeSuffix})"
							+ $", {nameof(timeSpan)}: {timeSpan}");
				}
			}

			IEnumerable<ValueTuple<string, TimeSpan?>> InvalidValues()
			{
				yield return ("", null);
				yield return ("a", null);
				yield return ("xyz", null);
				yield return ("1a", null);
				yield return ("z9", null);
				yield return ("qwerty", null);
			}
		}

		[Fact]
		public void Options_All_should_match_Snapshot_properties()
		{
			var p1 = typeof(MutableSnapshot).GetProperty(nameof(MutableSnapshot.LogLevelForTestingConfigParsing));
			var p2 = Options.LogLevelForTestingConfigParsing.MutableSnapshotPropertyInfo;
			(p1 == p2).Should().BeTrue();
			var a1 = new[] { p1 };
			var a2 = new[] { p2 };
			a1.ForEach(p => a2.Should().Contain(p));

			typeof(MutableSnapshot).GetProperties()
				.ForEach(propInfo => Options.All.Should()
					.Contain(optMeta => optMeta.MutableSnapshotPropertyInfo == propInfo
						, $"because propInfo.Name: {propInfo.Name}"));

			Options.All.Length.Should().Be(typeof(ISnapshot).GetProperties().Length);
		}

		[Fact]
		public void All_option_names_should_have_expected_prefix()
		{
			const string expectedPrefix = "ELASTIC_APM_TESTS_";
			Options.All.ForEach(optMeta => optMeta.Name.Should().StartWith(expectedPrefix));
		}

		[Fact]
		public void All_should_contain_all_static_XyzMetadata()
		{
			var allTestingConfigOptionsXyzMetadataStaticFields = typeof(Options)
				.GetFields(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public)
				.Where(p => typeof(Options.IOptionMetadata).IsAssignableFrom(p.FieldType))
				.ToArray();

			foreach (var fieldInfo in allTestingConfigOptionsXyzMetadataStaticFields)
			{
				Options.All.Should()
					.Contain(
						(Options.IOptionMetadata)fieldInfo.GetValue(null));
			}

			Options.All.Should().HaveCount(allTestingConfigOptionsXyzMetadataStaticFields.Length);
		}

		[Fact]
		public void All_options_should_have_different_names() => Options.All.ForEach(
			optMeta => Options.All.Where(x => optMeta.Name.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase))
				.Should()
				.HaveCount(1, $"because {nameof(optMeta)}: {optMeta}"));

		[Fact]
		public void All_options_should_refer_to_different_snapshot_properties() => Options.All.ForEach(
			optMeta => Options.All.Where(x => optMeta.MutableSnapshotPropertyInfo.Equals(x.MutableSnapshotPropertyInfo))
				.Should()
				.HaveCount(1, $"because {nameof(optMeta)}: {optMeta}"));

		[Fact]
		public void Option_without_value_should_be_set_to_default()
		{
			var allDefaultsConfigSnapshot = new MutableSnapshot(new MockRawConfigSnapshot(new Dictionary<string, string>()), XunitOutputHelper);
			Options.All.ForEach(optMeta =>
			{
				optMeta.MutableSnapshotPropertyInfo.GetValue(allDefaultsConfigSnapshot).Should().Be(optMeta.DefaultValueAsObject);
			});
		}

		[Fact]
		public void bool_option() =>
			TestOptionMetadata(
				PossibleBoolDefaultValues,
				PossibleStringToBoolVariants,
				(optionName, defaultValue) => new Options.BoolOptionMetadata(optionName, defaultValue, x => x.LogToConsoleEnabled));

		[Fact]
		public void string_option() =>
			TestOptionMetadata(
				PossibleStringDefaultValues,
				PossibleStringToStringVariants,
				(optionName, defaultValue) => new Options.StringOptionMetadata(optionName, defaultValue, x => x.LogToConsoleLinePrefix));

		[Fact]
		public void LogLevel_option() =>
			TestOptionMetadata(
				PossibleLogLevelDefaultValues,
				PossibleStringToLogLevelVariants,
				(optionName, defaultValue) => new Options.LogLevelOptionMetadata(optionName, defaultValue, x => x.LogLevel));

		[Fact]
		public void Nullable_int_option() =>
			TestOptionMetadata(
				PossibleNullableIntDefaultValues,
				PossibleStringToNullableIntVariants,
				(optionName, defaultValue) =>
					new Options.NullableIntOptionMetadata(optionName, defaultValue, x => x.RandomSeed));

		[Fact]
		public void TimeSpan_option()
		{
			foreach (var defaultTimeSuffix in PossibleTimeSuffixValues)
			{
				TestOptionMetadata(
					PossibleTimeSpanDefaultValues
					, GetPossibleStringToTimeSpanVariants(defaultTimeSuffix)
						.Select(((string timeSpanAsString, TimeSpan? timeSpan) t) =>
							(t.timeSpanAsString, t.timeSpan == null ? null : (object)t.timeSpan.Value))
					, (optionName, defaultValue) =>
						new Options.TimeSpanOptionMetadata(optionName, defaultValue, x => x.ReportLongRunningEvery, defaultTimeSuffix)
					, _logger
				);
			}
		}

		private static void TestOptionMetadata<T>(
			IEnumerable<T> possibleDefaultValues,
			IEnumerable<ValueTuple<string, object>> possibleStringToTVariants,
			Func<string, T, Options.OptionMetadata<T>> creator,
			IApmLogger loggerArg = null
		)
		{
			var logger = loggerArg ?? NoopLogger.Instance;
			const string optionName = "dummy_option_name";
			foreach (var defaultValue in possibleDefaultValues)
			{
				var optionMetadata = creator(optionName, defaultValue);
				// ReSharper disable once PossibleMultipleEnumeration
				foreach (var (stringValue, expectedTValue) in possibleStringToTVariants.Concat(new[] { ((string)null, (object)null) }))
				{
					logger.Error()?.Log("{stringValue} -> {expectedTValue}", stringValue.AsNullableToString(), expectedTValue.AsNullableToString());

					var configSnapshot =
						new MutableSnapshot(new MockRawConfigSnapshot(new Dictionary<string, string> { { optionName, stringValue } }),
							new[] { optionMetadata });

					optionMetadata.MutableSnapshotPropertyInfo.GetValue(configSnapshot)
						.Should()
						.Be(expectedTValue ?? defaultValue
							, $"because stringValue: {stringValue.AsNullableToString()}, expectedTValue: {expectedTValue.AsNullableToString()}"
							+ $", defaultValue: {defaultValue.AsNullableToString()}");
				}
			}
		}

		private class MockRawConfigSnapshot : IRawConfigSnapshot
		{
			private readonly IReadOnlyDictionary<string, string> _dictionary;

			internal MockRawConfigSnapshot(IReadOnlyDictionary<string, string> dictionary) => _dictionary = dictionary;

			public string DbgDescription => nameof(MockRawConfigSnapshot);

			public string GetValue(string optionName) => _dictionary.TryGetValue(optionName, out var optionValue) ? optionValue : null;
		}
	}
}
