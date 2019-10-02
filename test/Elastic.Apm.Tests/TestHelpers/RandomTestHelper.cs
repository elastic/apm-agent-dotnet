using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal class RandomTestHelper
	{
		internal readonly int Seed;
		private readonly ThreadLocal<Random> _threadLocal;

		internal RandomTestHelper(ITestOutputHelper xUnitOutputHelper, IApmLogger loggerForNonXunitSinks)
			: this(null, xUnitOutputHelper, loggerForNonXunitSinks) {}

		internal RandomTestHelper(int seed, ITestOutputHelper xUnitOutputHelper, IApmLogger loggerForNonXunitSinks)
			: this((int?)seed, xUnitOutputHelper, loggerForNonXunitSinks) {}

		private RandomTestHelper(int? seedArg, ITestOutputHelper xUnitOutputHelper, IApmLogger loggerForNonXunitSinks)
		{
			var (seed, dbgSeedDescription) = GetSeed(seedArg, xUnitOutputHelper);
			Seed = seed;
			LogSeed(Seed, xUnitOutputHelper, loggerForNonXunitSinks.Scoped(nameof(RandomTestHelper)), dbgSeedDescription);
			_threadLocal = new ThreadLocal<Random>(() => new Random(Seed));
		}

		private static (int, string) GetSeed(int? seedArg, ITestOutputHelper xUnitOutputHelper)
		{
			if (seedArg.HasValue) return (seedArg.Value, "passed as argument");

			var config = TestingConfig.ReadFromFromEnvVars(xUnitOutputHelper);
			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (config.RandomSeed.HasValue) return (config.RandomSeed.Value, "configured via environment variables");

			return (new Random().Next(), "randomly generated");
		}

		private static void LogSeed(int seed, ITestOutputHelper xUnitOutputHelper, IApmLogger loggerForNonXunitSinks, string dbgSeedDescription)
		{
			var message = $"Random generator seed: {seed} ({dbgSeedDescription})";
			xUnitOutputHelper.WriteLine(message);
			FindLevelToLog(loggerForNonXunitSinks)?.Let(foundLogLevel => loggerForNonXunitSinks.IfLevel(foundLogLevel)?.Log(message));
		}

		private static LogLevel? FindLevelToLog(IApmLogger logger) =>
			((LogLevel[])Enum.GetValues(typeof(LogLevel)))
			.OrderByDescending(x => x, new StricterLevelIsLowerComparator())
			.Where(logLevel =>
				new StricterLevelIsLowerComparator().Compare(logLevel, LogLevel.Information) <= 0 && logger.IsEnabled(logLevel))
			.Select(x => (LogLevel?)x)
			.FirstOrDefault();

		internal Random GetInstance() => _threadLocal.Value;

		internal class StricterLevelIsLowerComparator : IComparer<LogLevel>
		{
			public int Compare(LogLevel a, LogLevel b) => (LogLevel.Debug - LogLevel.Information) * (a - b);
		}
	}
}
