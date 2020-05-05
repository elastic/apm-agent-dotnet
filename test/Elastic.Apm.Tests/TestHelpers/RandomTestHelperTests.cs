// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class RandomTestHelperTests
	{
		[Fact]
		public void StricterLevelIsLowerComparator_tests()
		{
			var comparator = new RandomTestHelper.StricterLevelIsLowerComparator();
			comparator.Compare(LogLevel.Trace, LogLevel.Trace).Should().Be(0);
			comparator.Compare(LogLevel.Information, LogLevel.Debug).Should().BeNegative();
			comparator.Compare(LogLevel.Warning, LogLevel.Error).Should().BePositive();
		}

		[Fact]
		public void ctor_should_log_seed_with_default_level()
		{
			var mockXunitOutputHelper = new XunitOutputHelper();
			var mockLogger = new TestLogger(TestingConfig.Options.LogLevel.DefaultValue);
			var randomTestHelper = new RandomTestHelper(mockXunitOutputHelper, mockLogger);

			mockXunitOutputHelper.Lines.Should().ContainSingle();
			mockXunitOutputHelper.Lines.First().Should().MatchEquivalentOf($"*seed*{randomTestHelper.Seed}*generated*");
			mockLogger.Lines.Should().ContainSingle();
			mockLogger.Lines.First().Should().MatchEquivalentOf($"*{nameof(RandomTestHelper)}*seed*{randomTestHelper.Seed}*generated*");
		}

		[Fact]
		public void ctor_should_log_seed_with_any_level()
		{
			var logLevels = (LogLevel[])Enum.GetValues(typeof(LogLevel));
			foreach (var logLevel in logLevels)
			{
				var mockXunitOutputHelper = new XunitOutputHelper();
				var mockLogger = new TestLogger(logLevel);
				var randomTestHelper = new RandomTestHelper(mockXunitOutputHelper, mockLogger);

				mockXunitOutputHelper.Lines.Should().ContainSingle();
				mockXunitOutputHelper.Lines.First().Should().MatchEquivalentOf($"*seed*{randomTestHelper.Seed}*generated*");

				var expectedLogLevel = new RandomTestHelper.StricterLevelIsLowerComparator().Compare(logLevel, LogLevel.Information) <= 0
					? logLevel
					: LogLevel.Information;

				if (logLevel == LogLevel.None)
					mockLogger.Lines.Should().BeEmpty();
				else
				{
					mockLogger.Lines.Should().ContainSingle();
					mockLogger.Lines.First()
						.Should()
						.MatchEquivalentOf(
							$"*{ConsoleLogger.LevelToString(expectedLogLevel)}*{nameof(RandomTestHelper)}*seed*{randomTestHelper.Seed}*generated*");
				}
			}
		}

		[Fact]
		public void same_seed_reproduces_same_output()
		{
			const int numberOfRandomsToCheck = 10;
			var generatingRandomTestHelper = new RandomTestHelper(new NoopXunitOutputHelper(), new NoopLogger());
			var generatedRandoms = new double[numberOfRandomsToCheck];
			numberOfRandomsToCheck.Repeat(i => { generatedRandoms[i] = generatingRandomTestHelper.GetInstance().NextDouble(); });

			var mockXunitOutputHelper = new XunitOutputHelper();
			var mockLogger = new TestLogger();
			var reproducingRandomTestHelper = new RandomTestHelper(generatingRandomTestHelper.Seed, mockXunitOutputHelper, mockLogger);

			mockXunitOutputHelper.Lines.Should().ContainSingle();
			mockXunitOutputHelper.Lines.First().Should().MatchEquivalentOf($"*seed*{generatingRandomTestHelper.Seed}*passed*argument*");

			mockLogger.Lines.Should().ContainSingle();
			mockLogger.Lines.First()
				.Should()
				.MatchEquivalentOf(
					$"*{nameof(RandomTestHelper)}*seed*{generatingRandomTestHelper.Seed}*passed*argument*");

			// ReSharper disable ImplicitlyCapturedClosure
			numberOfRandomsToCheck.Repeat(i => { reproducingRandomTestHelper.GetInstance().NextDouble().Should().Be(generatedRandoms[i]); });
			// ReSharper restore ImplicitlyCapturedClosure
		}

		private class NoopXunitOutputHelper : ITestOutputHelper
		{
			public void WriteLine(string line) { }

			public void WriteLine(string format, params object[] args) { }
		}

		private class XunitOutputHelper : ITestOutputHelper
		{
			internal readonly List<string> Lines = new List<string>();

			public void WriteLine(string line) => Lines.Add(line);

			public void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));
		}
	}
}
