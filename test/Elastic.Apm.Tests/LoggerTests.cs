using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LoggerTests
	{
		[Fact]
		public void TestLogError()
		{
			var logger = LogWithLevel(LogLevel.Error);

			logger.Lines.Should().ContainSingle();
			logger.Lines[0].Should().EndWith("[Error] - Error log");
		}

		[Fact]
		public void TestLogWarning()
		{
			var logger = LogWithLevel(LogLevel.Warning);

			logger.Lines.Count.Should().Be(2);
			logger.Lines[0].Should().EndWith("[Error] - Error log");
			logger.Lines[1].Should().EndWith("[Warning] - Warning log");
		}

		[Fact]
		public void TestLogInfo()
		{
			var logger = LogWithLevel(LogLevel.Information);

			logger.Lines.Count.Should().Be(3);
			logger.Lines[0].Should().EndWith("[Error] - Error log");
			logger.Lines[1].Should().EndWith("[Warning] - Warning log");
			logger.Lines[2].Should().EndWith("[Info] - Info log");
		}

		[Fact]
		public void TestLogDebug()
		{
			var logger = LogWithLevel(LogLevel.Debug);

			logger.Lines.Count.Should().Be(4);
			logger.Lines[0].Should().EndWith("[Error] - Error log");
			logger.Lines[1].Should().EndWith("[Warning] - Warning log");
			logger.Lines[2].Should().EndWith("[Info] - Info log");
			logger.Lines[3].Should().EndWith("[Debug] - Debug log");
		}

		/// <summary>
		/// Logs a message with exception by using <see cref="LoggingExtensions.MaybeLogger.LogException" />.
		/// Makes sure that both the message and the exception is printed.
		/// First the Message is printed and the exception is in the last line.
		/// </summary>
		[Fact]
		public void LogExceptionTest()
		{
			var logger = new TestLogger(LogLevel.Warning);

			logger.Warning()
				?.LogException(
					new Exception("Something went wrong"),
					$"Failed sending events. Following events were not transferred successfully to the server:{Environment.NewLine}{{items}}",
					string.Join($",{Environment.NewLine}", new List<string> { "Item1", "Item2", "Item3" }));

			logger.Lines[0]
				.Should()
				.Contain("Failed sending events. Following events were not transferred successfully to the server:");

			logger.Lines[1].Should().Contain("Item1");
			logger.Lines[2].Should().Contain("Item2");
			logger.Lines[3].Should().Contain("Item3");

			logger.Lines.Last()
				.Should()
				.ContainAll(new List<string> { "System.Exception", "Something went wrong" });
		}

		[Fact]
		public void LogException_should_print_stack_trace_including_for_inner_exceptions()
		{
			const string msg5 = "Message for exception thrown from func #5";
			const string msg3 = "Message for exception rethrown from func #3";
			const string msg1 = "Message for exception rethrown from func #1";
			const string capturingLogMsgArg = "Capturing log message arg";

			void LogExceptionShouldIncludeStackTraceFunc1()
			{
				try
				{
					LogExceptionShouldIncludeStackTraceFunc2();
				}
				catch (Exception ex)
				{
					throw new Exception(msg1, ex);
				}
			}

			void LogExceptionShouldIncludeStackTraceFunc2()
			{
				LogExceptionShouldIncludeStackTraceFunc3();
			}

			void LogExceptionShouldIncludeStackTraceFunc3()
			{
				try
				{
					LogExceptionShouldIncludeStackTraceFunc4();
				}
				catch (Exception ex)
				{
					throw new Exception(msg3, ex);
				}
			}

			void LogExceptionShouldIncludeStackTraceFunc4()
			{
				LogExceptionShouldIncludeStackTraceFunc5();
			}

			void LogExceptionShouldIncludeStackTraceFunc5()
			{
				throw new Exception(msg5);
			}

			var logger = new TestLogger(LogLevel.Warning);

			try
			{
				LogExceptionShouldIncludeStackTraceFunc1();
			}
			catch (Exception ex)
			{
				logger.Warning()?.LogException(ex, "Exception has been thrown. Arg: {Arg}", capturingLogMsgArg);
			}

			logger.Lines.Should().Contain(line => line.Contains(capturingLogMsgArg));

			logger.Lines.Should().Contain(line => line.Contains(msg1));
			logger.Lines.Should().Contain(line => line.Contains(msg3));
			logger.Lines.Should().Contain(line => line.Contains(msg5));

			5.Repeat(i => { logger.Lines.Should().Contain(line => line.Contains($"LogExceptionShouldIncludeStackTraceFunc{i + 1}")); });
			logger.Lines.Should().NotContain(line => line.Contains("LogExceptionShouldIncludeStackTraceFunc6"));
		}

		private static TestLogger LogWithLevel(LogLevel logLevel)
		{
			var logger = new TestLogger(logLevel);

			logger.Error()?.Log("Error log");
			logger.Warning()?.Log("Warning log");
			logger.Info()?.Log("Info log");
			logger.Debug()?.Log("Debug log");
			return logger;
		}
	}
}
