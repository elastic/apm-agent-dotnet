using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class LoggingTestBase : IDisposable
	{
		internal static readonly LogLevel DefaultLogLevel = ConsoleLogger.DefaultLogLevel;

		private static readonly ThreadSafeLongCounter TestIdCounter = new ThreadSafeLongCounter();

		protected readonly IApmLogger Logger;
		protected readonly IApmLogger LoggerForNonXunitSinks;
		protected readonly string TestDisplayName;
		protected readonly ITestOutputHelper XunitOutputHelper;

		protected LoggingTestBase(ITestOutputHelper xUnitOutputHelper, string derivedClassName = null)
		{
			XunitOutputHelper = xUnitOutputHelper;

			var sinkWriters = new List<ILineWriter>();

			var testId = TestIdCounter.Increment();

			var config = TestingConfig.ReadFromFromEnvVars(xUnitOutputHelper);

			if (config.LogToSysDiagTraceEnabled)
				sinkWriters.Add(new SystemDiagnosticsTraceLineWriter(string.Format(config.LogToSysDiagTraceLinePrefix, testId)));

			if (config.LogToConsoleEnabled)
				sinkWriters.Add(new SystemDiagnosticsTraceLineWriter(string.Format(config.LogToConsoleLinePrefix, testId)));

			LoggerForNonXunitSinks = new LineWriterToLoggerAdaptor(new SplittingLineWriter(sinkWriters.ToArray()), config.LogLevel);
			if (derivedClassName != null) LoggerForNonXunitSinks = LoggerForNonXunitSinks.Scoped(derivedClassName);
			TestDisplayName = GetTestDisplayName(xUnitOutputHelper);
			LoggerForNonXunitSinks.Info()?.Log("Starting test: {UnitTestDisplayName}", TestDisplayName);

			sinkWriters.Add(new XunitOutputToLineWriterAdaptor(xUnitOutputHelper));
			Logger = new LineWriterToLoggerAdaptor(new SplittingLineWriter(sinkWriters.ToArray()), config.LogLevel);
			if (derivedClassName != null) Logger = Logger.Scoped(derivedClassName);
		}

		public void Dispose() => LoggerForNonXunitSinks.Info()?.Log("Finished test: {UnitTestDisplayName}", TestDisplayName);

		private static string GetTestDisplayName(ITestOutputHelper xUnitOutputHelper)
		{
			var helper = (TestOutputHelper)xUnitOutputHelper;

			var test = (ITest)helper.GetType()
				.GetField("test", BindingFlags.NonPublic | BindingFlags.Instance)
				?.GetValue(helper);
			return test?.DisplayName;
		}

	}
}
