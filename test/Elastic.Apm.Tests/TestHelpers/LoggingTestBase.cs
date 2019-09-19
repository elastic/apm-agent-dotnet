using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class LoggingTestBase : IDisposable
	{
		private const string ThisClassName = nameof(LoggingTestBase);

		private static readonly ThreadSafeLongCounter TestIdCounter = new ThreadSafeLongCounter();

		protected readonly IApmLogger LoggerBase;

		private readonly ITest _currentXunitTest;
		private readonly IApmLogger _loggerForStartFinish;

		protected LoggingTestBase(ITestOutputHelper xUnitOutputHelper)
		{
			_currentXunitTest = GetCurrentXunitTest(xUnitOutputHelper);

			var lineWriters = new List<ILineWriter>();

			var testId = TestIdCounter.Increment();

			var config = TestingConfig.ReadFromFromEnvVars(xUnitOutputHelper);

			if (config.LogToSysDiagTraceEnabled)
				lineWriters.Add(new SystemDiagnosticsTraceLineWriter(string.Format(config.LogToSysDiagTraceLinePrefix, testId)));

			if (config.LogToConsoleEnabled)
				lineWriters.Add(new FlushingTextWriterToLineWriterAdaptor(Console.Out, string.Format(config.LogToConsoleLinePrefix, testId)));

			var writerForStartFinish = lineWriters.ToArray();
			if (config.LogToXunitEnabled)
			{
				lineWriters.Add(new XunitOutputToLineWriterAdaptor(xUnitOutputHelper, string.Format(config.LogToXunitLinePrefix, testId)));
				if (!TestingConfig.IsRunningInIde) writerForStartFinish = lineWriters.ToArray();
			}

			_loggerForStartFinish = new LineWriterToLoggerAdaptor(new SplittingLineWriter(writerForStartFinish), config.LogLevel)
				.Scoped(ThisClassName);

			_loggerForStartFinish.Info()?.Log("Starting test: {UnitTestDisplayName}...", TestDisplayName);

			LoggerBase = new LineWriterToLoggerAdaptor(new SplittingLineWriter(lineWriters.ToArray()), config.LogLevel);
		}

		protected string TestDisplayName => _currentXunitTest?.DisplayName;

		public virtual void Dispose() => _loggerForStartFinish.Info()?.Log("Finished test: {UnitTestDisplayName}", TestDisplayName);

		internal static ITest GetCurrentXunitTest(ITestOutputHelper xUnitOutputHelper)
		{
			var helper = (TestOutputHelper)xUnitOutputHelper;
			var helperTestFieldInfo = helper.GetType().GetField("test", BindingFlags.NonPublic | BindingFlags.Instance);
			var helperTestFieldValue = helperTestFieldInfo?.GetValue(helper);
			return (ITest)helperTestFieldValue;
		}
	}
}
