using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Extensions;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Tests.TestHelpers.TestingConfig;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class TestingConfigTests
	{
		private readonly ITestOutputHelper _xunitOutputHelper;

		private static readonly IEnumerable<bool> PossibleBoolDefaultValues = new[] { false, true };

		private static readonly IEnumerable<ValueTuple<string, object>> PossibleStringToBoolVariants = new[]
		{
			("false", (object)false), ("true", true), ("FALSE", false), ("tRUe", true), ("", null), ("qwerty", null)
		};

		private static readonly IEnumerable<string> PossibleStringDefaultValues = new[]
		{
			null, "", "1", "22", "123", "some default value", "with various \t white \n\t space"
		};

		private static IEnumerable<ValueTuple<string, object>> PossibleStringToStringVariants =>
			PossibleStringDefaultValues.Select(defaultValue => (defaultValue, (object)defaultValue));

		private static readonly IEnumerable<LogLevel> PossibleLogLevelDefaultValues = (LogLevel[]) Enum.GetValues(typeof(LogLevel));

		private static readonly IEnumerable<ValueTuple<string, object>> PossibleStringToLogLevelVariants = GetPossibleStringToLogLevelVariants();

		private static IEnumerable<ValueTuple<string, object>> GetPossibleStringToLogLevelVariants()
		{
			var counter = 0;
			foreach (var defaultValue in PossibleLogLevelDefaultValues)
			{
				var defaultValueAsString = defaultValue.ToString();

				yield return (defaultValueAsString, (object)defaultValue);
				yield return (defaultValueAsString.ToLower(), (object)defaultValue);
				yield return (defaultValueAsString.ToUpper(), (object)defaultValue);
				yield return (UppercaseLetterAt(defaultValueAsString, counter % defaultValueAsString.Length), (object)defaultValue);

				yield return (defaultValueAsString.Remove(counter % defaultValueAsString.Length), (object)null);

				++counter;
			}

			var invalidLogLevelStrings = new []{ "", "x", "some text", "with various \t white \n\t space" };
			foreach (var invalidLogLevelString in invalidLogLevelStrings) yield return (invalidLogLevelString, (object)null);

			string UppercaseLetterAt(string str, int index) =>
				(index > 0 ? str.Substring(0, index) : "")
				+ char.ToUpper(str[index], CultureInfo.InvariantCulture)
				+ (index < (str.Length - 1) ? str.Substring(index + 1) : "");
		}

		private static readonly IEnumerable<int?> PossibleNullableIntDefaultValues = new[] { 0, 1, 22, 123, (int?)null };

		private static readonly IEnumerable<ValueTuple<string, object>> PossibleStringToNullableIntVariants = GetPossibleStringToNullableIntVariants();

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

		public TestingConfigTests(ITestOutputHelper xunitOutputHelper) => _xunitOutputHelper = xunitOutputHelper;

		[Fact]
		public void Options_All_should_match_Snapshot_properties()
		{
			Options.All.Length.Should().Be(typeof(Snapshot).GetProperties().Length);
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
		}

		[Fact]
		public void All_option_names_should_have_expected_prefix()
		{
			const string expectedPrefix = "ELASTIC_APM_TESTS_";
			Options.All.ForEach(optMeta => optMeta.Name.Should().StartWith(expectedPrefix));
		}

		[Fact]
		public void Option_without_value_should_be_set_to_default()
		{
			var allDefaultsConfigSnapshot = new MutableSnapshot(new MockRawConfigSnapshot(new Dictionary<string, string>()), _xunitOutputHelper);
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
		public void log_level_option() =>
			TestOptionMetadata(
				PossibleLogLevelDefaultValues,
				PossibleStringToLogLevelVariants,
				(optionName, defaultValue) => new Options.LogLevelOptionMetadata(optionName, defaultValue, x => x.LogLevel));

		[Fact]
		public void nullable_int_option() =>
			TestOptionMetadata(
				PossibleNullableIntDefaultValues,
				PossibleStringToNullableIntVariants,
				(optionName, defaultValue) =>
					new Options.NullableIntOptionMetadata(optionName, defaultValue, x => x.RandomSeed));

		private void TestOptionMetadata<T>(
			IEnumerable<T> possibleDefaultValues,
			IEnumerable<ValueTuple<string, object>> possibleStringToTVariants,
			Func<string, T, Options.OptionMetadata<T>> creator
		)
		{
			const string optionName = "dummy_option_name";
			foreach (var defaultValue in possibleDefaultValues)
			{
				var optionMetadata = creator(optionName, defaultValue);
				foreach (var (stringValue, expectedTValue) in possibleStringToTVariants.Concat(new []{ ((string)null, (object)null) }))
				{
					var configSnapshot =
						new MutableSnapshot(new MockRawConfigSnapshot(new Dictionary<string, string> { { optionName, stringValue } }),
							new [] { optionMetadata });

					optionMetadata.MutableSnapshotPropertyInfo.GetValue(configSnapshot).Should().Be(expectedTValue ?? defaultValue
						, $"because stringValue: {stringValue.AsNullableToString()}, expectedTValue: {expectedTValue.AsNullableToString()}"
						+ $", defaultValue: {defaultValue.AsNullableToString()}");
				}
			}
		}

		private class MockRawConfigSnapshot : RawConfigSnapshot
		{
			private readonly IReadOnlyDictionary<string, string> _dictionary;

			internal MockRawConfigSnapshot(IReadOnlyDictionary<string, string> dictionary) => _dictionary = dictionary;

			public string DbgDescription => nameof(MockRawConfigSnapshot);

			public string GetValue(string optionName) => _dictionary.TryGetValue(optionName, out var optionValue) ? optionValue : null;
		}
	}
}
