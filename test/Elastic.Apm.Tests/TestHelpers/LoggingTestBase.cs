// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

		protected readonly ITestOutputHelper TestOutputHelper;
		protected readonly ITest CurrentXunitTest;
		private readonly LineWriterToLoggerAdaptor _loggerForStartFinish;

		protected LoggingTestBase(ITestOutputHelper xUnitOutputHelper)
		{
			TestOutputHelper = xUnitOutputHelper;
			CurrentXunitTest = GetCurrentXunitTest(xUnitOutputHelper);

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

			_loggerForStartFinish = new LineWriterToLoggerAdaptor(new SplittingLineWriter(writerForStartFinish), config.LogLevel);

			LogTestStartFinish( /* isStart: */ true);

			LoggerBase = new LineWriterToLoggerAdaptor(new SplittingLineWriter(lineWriters.ToArray()), config.LogLevel);
		}

		protected string TestDisplayName => CurrentXunitTest?.DisplayName;

		public virtual void Dispose() => LogTestStartFinish( /* isStart: */ false);

		private void LogTestStartFinish(bool isStart)
		{
			var originalLogLevel = _loggerForStartFinish.Level;
			_loggerForStartFinish.Level = LogLevel.Information;
			_loggerForStartFinish.Scoped(ThisClassName)
				.Info()
				?.Log(
					isStart ? "Starting test: {UnitTestDisplayName}..." : "Finished test: {UnitTestDisplayName}", TestDisplayName);
			_loggerForStartFinish.Level = originalLogLevel;
		}

		internal static ITest GetCurrentXunitTest(ITestOutputHelper xUnitOutputHelper)
		{
			var helper = (TestOutputHelper)xUnitOutputHelper;
			var helperTestFieldInfo = helper.GetType().GetField("test", BindingFlags.NonPublic | BindingFlags.Instance);
			var helperTestFieldValue = helperTestFieldInfo?.GetValue(helper);
			return (ITest)helperTestFieldValue;
		}
	}
}
