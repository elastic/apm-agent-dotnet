using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class LoggingTestBase : IDisposable
	{
		private const string ThisClassName = nameof(LoggingTestBase);
		private static readonly LazyContextualInit<TestingConfig.ISnapshot> CachedConfigSingleton = new LazyContextualInit<TestingConfig.ISnapshot>();

		private static readonly ThreadSafeLongCounter TestIdCounter = new ThreadSafeLongCounter();
		protected readonly LineWriterToLoggerAdaptor LoggerBase;
		protected readonly ITestOutputHelper XunitOutputHelper;

		private readonly TestingConfig.ISnapshot _config;

		private readonly ITest _currentXunitTest;
		private readonly LineWriterToLoggerAdaptor _loggerForStartFinish;
		private readonly LongRunningReporter _longRunningReporter;

		protected LoggingTestBase(ITestOutputHelper xUnitOutputHelper, LogLevel? overridingLogLevel = null)
		{
			XunitOutputHelper = xUnitOutputHelper;
			_currentXunitTest = GetCurrentXunitTest(xUnitOutputHelper);

			var lineWriters = new List<ILineWriter>();

			var testId = TestIdCounter.Increment();

			_config = CachedConfigSingleton.IfNotInited?.InitOrGet(() => TestingConfig.ReadFromFromEnvVars(xUnitOutputHelper))
				?? CachedConfigSingleton.Value;

			var logLevel = overridingLogLevel ?? _config.LogLevel;

			if (_config.LogToSysDiagTraceEnabled)
				lineWriters.Add(new SystemDiagnosticsTraceLineWriter(string.Format(_config.LogToSysDiagTraceLinePrefix, testId)));

			if (_config.LogToConsoleEnabled)
				lineWriters.Add(new FlushingTextWriterToLineWriterAdaptor(Console.Out, string.Format(_config.LogToConsoleLinePrefix, testId)));

			var writerForStartFinish = lineWriters.ToArray();
			if (_config.LogToXunitEnabled)
			{
				lineWriters.Add(new XunitOutputToLineWriterAdaptor(xUnitOutputHelper, string.Format(_config.LogToXunitLinePrefix, testId)));
				if (!TestingConfig.IsRunningInIde) writerForStartFinish = lineWriters.ToArray();
			}

			_loggerForStartFinish = new LineWriterToLoggerAdaptor(new SplittingLineWriter(writerForStartFinish), logLevel);

			LogTestStartFinish( /* isStart: */ true);

			LoggerBase = new LineWriterToLoggerAdaptor(new SplittingLineWriter(lineWriters.ToArray()), logLevel);

			_longRunningReporter = _config.ReportLongRunningEnabled ? new LongRunningReporter(this) : null;
		}

		protected string TestDisplayName => _currentXunitTest?.DisplayName;

		public virtual void Dispose()
		{
			_longRunningReporter?.Dispose();

			LogTestStartFinish( /* isStart: */ false);
		}

		private void LogTestStartFinish(bool isStart) =>
			LogStatusInfo(logger =>
			{
				logger.Scoped(ThisClassName)
					.Info()
					?.Log("Test " + (isStart ? "started" : "finished")
						+ ". Test display name: `{UnitTestDisplayName}'. Testing configuration: {TestingConfig}"
						, TestDisplayName, _config);
			});

		private void LogStatusInfo(Action<IApmLogger> loggingAction)
		{
			var originalLogLevel = _loggerForStartFinish.Level;
			_loggerForStartFinish.Level = LogLevel.Information;
			loggingAction(_loggerForStartFinish);
			_loggerForStartFinish.Level = originalLogLevel;
		}

		internal static ITest GetCurrentXunitTest(ITestOutputHelper xUnitOutputHelper)
		{
			var helper = (TestOutputHelper)xUnitOutputHelper;
			var helperTestFieldInfo = helper.GetType().GetField("test", BindingFlags.NonPublic | BindingFlags.Instance);
			var helperTestFieldValue = helperTestFieldInfo?.GetValue(helper);
			return (ITest)helperTestFieldValue;
		}

		private class LongRunningReporter : IDisposable
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private const string ThisClassName = LoggingTestBase.ThisClassName + "." + nameof(LongRunningReporter);
			private readonly CancellationTokenSource _cts = new CancellationTokenSource();
			private readonly object _lock = new object();

			private readonly LoggingTestBase _owner;
			private readonly Stopwatch _stopwatch;

			public LongRunningReporter(LoggingTestBase owner)
			{
				_owner = owner;
				_stopwatch = Stopwatch.StartNew();

				Task.Run(async () => { await ExceptionUtils.DoSwallowingExceptions(new NoopLogger(), ReportingLoop); });
			}

			private bool _isDisposed;

			public void Dispose()
			{
				lock (_lock)
				{
					if (_isDisposed) return;

					_cts.Cancel();

					_isDisposed = true;
				}
			}

			private async Task ReportingLoop()
			{
				lock (_lock)
				{
					if (_isDisposed)
						return;
				}

				await Task.Delay(_owner._config.ReportLongRunningAfter, _cts.Token);

				while (true)
				{
					lock (_lock)
					{
						if (_isDisposed) return;

						_owner.LogStatusInfo(logger =>
						{
							logger.Scoped(ThisClassName)
								.Warning()
								?.Log("Long running test detected. Time elapsed since test started: {TestDuration}."
									+ " Test display name: `{UnitTestDisplayName}'."
									+ Environment.NewLine + "+-> Logger context:{LoggerContext}"
									, _stopwatch.Elapsed.ToHmsInSeconds(), _owner.TestDisplayName
									, FormatLoggerContext(_owner.LoggerBase.Context.Copy()));
						});
					}

					await Task.Delay(_owner._config.ReportLongRunningEvery, _cts.Token);
				}

				string FormatLoggerContext(IReadOnlyDictionary<string, string> loggerCtx)
				{
					if (loggerCtx.IsEmpty()) return " <EMPTY>";

					var itemsListStrBuilder = new StringBuilder();
					// ReSharper disable once UseDeconstructionOnParameter
					loggerCtx.ForEachIndexed((kv, index) =>
					{
						if (index != 0) itemsListStrBuilder.Append(Environment.NewLine);
						itemsListStrBuilder.Append($"[{index + 1}]: `{kv.Key}': `{kv.Value}'");
					});

					return $" {loggerCtx.Count} items:" + Environment.NewLine + TextUtils.Indent(itemsListStrBuilder.ToString());
				}
			}
		}
	}
}
