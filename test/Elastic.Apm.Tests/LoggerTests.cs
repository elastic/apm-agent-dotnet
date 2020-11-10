// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
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

		/// <summary>
		/// Makes sure the logger does not throw in case of templates for structured logs with non-existing corresponding values.
		/// This test uses scoped logger.
		/// </summary>
		[Fact]
		public void StructuredLogTemplateWith1MissingArgument_ScopedLogger()
		{
			var consoleLogger = new ConsoleLogger(LogLevel.Trace, TextWriter.Null, TextWriter.Null);
			var scopedLogger = consoleLogger.Scoped("MyTestScope");

			const string arg1Value = "testArgumentValue";
			scopedLogger.Warning()?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1} {arg2}", arg1Value);
		}

		/// <summary>
		/// Makes sure the logger does not throw in case of templates for structured logs with too much arguments - in other words
		/// with argument(s) that do not have
		/// corresponding placeholders in the template.
		/// This test uses scoped logger.
		/// </summary>
		[Fact]
		public void StructuredLogTemplateWithAdditionalArguments()
		{
			var consoleLogger = new ConsoleLogger(LogLevel.Trace, TextWriter.Null, TextWriter.Null);

			const string arg1Value = "testArgumentValue1";
			const string arg2Value = "testArgumentValue2";
			const string arg3Value = "testArgumentValue3";
			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1} {arg2}", arg1Value, arg2Value,
					arg3Value);

			const string arg4Value = "testArgumentValue4";
			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1} {arg2}", arg1Value, arg2Value,
					arg3Value, arg4Value);
		}

		/// <summary>
		/// Makes sure the logger does not throw in case of templates for structured logs with non-existing corresponding values.
		/// </summary>
		[Fact]
		public void StructuredLogTemplateWith1MissingArgument()
		{
			var consoleLogger = new ConsoleLogger(LogLevel.Trace, TextWriter.Null, TextWriter.Null);

			const string arg1Value = "testArgumentValue";
			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1} {arg2}", arg1Value);
		}

		/// <summary>
		/// Makes sure than unbalanced braces don't make the logger throw
		/// </summary>
		[Fact]
		public void StructuredLogWithUnbalancedBraces()
		{
			var consoleLogger = new TestLogger(LogLevel.Trace);

			const string arg1Value = "testArgumentValue1";
			const string arg2Value = "testArgumentValue2";

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {{arg1} {arg2}", arg1Value, arg2Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1}} {arg2}", arg1Value, arg2Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1}} {arg2}", arg1Value, arg2Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1 {arg2}}", arg1Value, arg2Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1 {arg2}}}", arg1Value, arg2Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1 {arg2}", arg1Value, arg2Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1", arg1Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: arg1}", arg1Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: arg1}}", arg1Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {{arg1", arg1Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {{arg1}", arg1Value);

			consoleLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument, args: {arg1}}", arg1Value);
		}

		/// <summary>
		/// Makes sure the logger does not throw in case of templates for structured logs with too much arguments - in other words
		/// with argument(s) that do not have
		/// corresponding placeholders in the template
		/// </summary>
		[Fact]
		public void StructuredLogTemplateWithAdditionalArguments_ScopedLogger()
		{
			var consoleLogger = new ConsoleLogger(LogLevel.Trace, TextWriter.Null, TextWriter.Null);
			var scopedLogger = consoleLogger.Scoped("MyTestScope");

			const string arg1Value = "testArgumentValue1";
			const string arg2Value = "testArgumentValue2";
			const string arg3Value = "testArgumentValue3";
			scopedLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWithAdditionalArguments_ScopedLogger, args: {arg1} {arg2}", arg1Value,
					arg2Value,
					arg3Value);

			const string arg4Value = "testArgumentValue4";
			scopedLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWithAdditionalArguments_ScopedLogger, args: {arg1} {arg2}", arg1Value,
					arg2Value,
					arg3Value, arg4Value);
		}

		/// <summary>
		/// Makes sure the error caused by argument mismatch is properly logged
		/// </summary>
		[Fact]
		public void StructuredLogTemplateWithAdditionalArguments_LogError()
		{
			var testLogger = new TestLogger();

			const string arg1Value = "testArgumentValue1";
			const string arg2Value = "testArgumentValue2";
			const string arg3Value = "testArgumentValue3";
			const string arg4Value = "testArgumentValue4";
			testLogger.Error()
				?.Log("This is a test log from the test StructuredLogTemplateWithAdditionalArguments_LogError, args: {arg1} {arg2}", arg1Value,
					arg2Value, arg3Value, arg4Value);

			testLogger.Lines[0]
				.Should()
				.Contain(
					"Warning: This line is from an invalid structured log which should be fixed and may not be complete: number of placeholders in the log message does not match the number of parameters.");
			testLogger.Lines[0]
				.Should()
				.Contain(
					"This is a test log from the test StructuredLogTemplateWithAdditionalArguments_LogError, args: testArgumentValue1 testArgumentValue2");

			testLogger.Lines[0]
				.Should()
				.Contain("Argument values without placeholders: testArgumentValue3, testArgumentValue4");
		}

		/// <summary>
		/// Makes sure the error caused by argument mismatch is properly logged
		/// </summary>
		[Fact]
		public void StructuredLogTemplateWith1MissingArgument_LogError()
		{
			var testLogger = new TestLogger(LogLevel.Trace);

			const string arg1Value = "testArgumentValue";
			testLogger.Warning()
				?.Log("This is a test log from the test StructuredLogTemplateWith1MissingArgument_LogError, args: {arg1} {arg2}", arg1Value);

			testLogger.Lines[0]
				.Should()
				.Contain(
					"Warning: This line is from an invalid structured log which should be fixed and may not be complete: number of arguments is not matching the number of placeholders, placeholders with missing values: arg2");

			testLogger.Lines[0]
				.Should()
				.Contain(
					"This is a test log from the test StructuredLogTemplateWith1MissingArgument_LogError, args: testArgumentValue");
		}

		/// <summary>
		/// Makes sure that a structured log with the proper number of arguments does not contain any warning about being invalid.
		/// </summary>
		[Fact]
		public void StructuredLogWithCorrectNumberOfArgs()
		{
			var testLogger = new TestLogger(LogLevel.Trace);
			const string arg1Value = "testArgumentValue1";
			const string arg2Value = "testArgumentValue2";
			testLogger.Warning()
				?.Log("This is a test log from the test StructuredLogWithCorrectNumberOfArgs, args: {arg1} {arg2}", arg1Value, arg2Value);

			testLogger.Lines[0]
				.Should()
				.Contain(
					"This is a test log from the test StructuredLogWithCorrectNumberOfArgs, args: testArgumentValue1 testArgumentValue2");

			testLogger.Lines[0]
				.Should()
				.NotContain(
					"Warning: This line is from an invalid structured log");

			testLogger.Lines[0]
				.Should()
				.NotContain(
					"Argument values without placeholders:");

			testLogger.Lines[0]
				.Should()
				.NotContain(
					"placeholders with missing values:");
		}

		/// <summary>
		/// Makes sure that getting the current transaction does not log anything.
		/// Reason for this is that <code>Tracer.CurrentTransaction</code> is used in log correlation and within log correlation
		/// the agent should not log. See: https://github.com/elastic/ecs-dotnet/issues/58#issuecomment-595864256
		/// </summary>
		[Fact]
		public void GetCurrentTransactionNoLogging()
		{
			var testLogger = new TestLogger(LogLevel.Trace);
			using var agent = new ApmAgent(new TestAgentComponents(testLogger));
			agent.Tracer.CaptureTransaction("TestTransaction", "Test", () =>
			{
				var numberOfLinesPreCurrentTransaction = testLogger.Lines.Count;
				// ReSharper disable once AccessToDisposedClosure
				var currentTransaction = agent.Tracer.CurrentTransaction;
				currentTransaction.Should().NotBeNull();
				testLogger.Lines.Count.Should().Be(numberOfLinesPreCurrentTransaction);
			});
		}

		/// <summary>
		/// Initializes a <see cref="PayloadSenderV2" /> with a server url which contains basic authentication.
		/// In this test the server does not exist. The test makes sure that the user name and password from basic auth. is not
		/// printed in the logs.
		/// </summary>
		[Fact]
		public void PayloadSenderNoUserNamePwPrintedForServerUrl()
		{
			var userName = "abc";
			var pw = "def";
			var inMemoryLogger = new InMemoryBlockingLogger(LogLevel.Warning);
			var configReader = new MockConfigSnapshot(serverUrls: $"http://{userName}:{pw}@localhost:8234", maxBatchEventCount: "0",
				flushInterval: "0");

			using var payloadSender = new PayloadSenderV2(inMemoryLogger, configReader,
				Service.GetDefaultService(configReader, inMemoryLogger), new Api.System(), new MockServerInfo());

			using var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction("Test", "TestTransaction", () => { });

			inMemoryLogger.Lines.Should().HaveCount(1);
			inMemoryLogger.Lines.Should().NotContain(n => n.Contains($"{userName}:{pw}"));
			inMemoryLogger.Lines.Should().Contain(n => n.Contains("http://[REDACTED]:[REDACTED]@localhost:8234"));
		}

		/// <summary>
		/// Initializes a <see cref="PayloadSenderV2" /> with a server url which contains basic authentication.
		/// In this test the server exists and return HTTP 500.
		/// The test makes sure that the user name and password from basic auth. is not printed in the logs.
		/// </summary>
		[Fact]
		public void PayloadSenderNoUserNamePwPrintedForServerUrlWithServerReturn()
		{
			var userName = "abc";
			var pw = "def";
			var inMemoryLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var port = new Random(DateTime.UtcNow.Millisecond).Next(8100, 65535);
			var configReader = new MockConfigSnapshot(serverUrls: $"http://{userName}:{pw}@localhost:{port}", maxBatchEventCount: "0",
				flushInterval: "0");

			using var payloadSender = new PayloadSenderV2(inMemoryLogger, configReader,
				Service.GetDefaultService(configReader, inMemoryLogger), new Api.System(), new MockServerInfo());

			using var localServer = new LocalServer(httpListenerContext => { httpListenerContext.Response.StatusCode = 500; },
				$"http://localhost:{port}/");

			using var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction("Test", "TestTransaction", () => { });

			inMemoryLogger.Lines.Should().HaveCount(1);
			inMemoryLogger.Lines.Should().NotContain(n => n.Contains($"{userName}:{pw}"));
			inMemoryLogger.Lines.Should().Contain(n => n.Contains($"http://[REDACTED]:[REDACTED]@localhost:{port}"));
		}

		/// <summary>
		/// Initializes a <see cref="CentralConfigFetcher" /> with a server url which contains basic authentication.
		/// The test makes sure that the user name and password from basic auth. is not printed in the logs on error level.
		/// </summary>
		[Fact]
		public void CentralConfigNoUserNamePwPrinted()
		{
			var userName = "abc";
			var pw = "def";

			var inMemoryLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var configReader = new MockConfigSnapshot(serverUrls: $"http://{userName}:{pw}@localhost:8123", maxBatchEventCount: "0",
				flushInterval: "0");

			var configStore = new ConfigStore(configReader, inMemoryLogger);
			using var centralConfigFetcher =
				new CentralConfigFetcher(inMemoryLogger, configStore, Service.GetDefaultService(configReader, inMemoryLogger));

			inMemoryLogger.Lines.Should().HaveCount(1);
			inMemoryLogger.Lines.Should().NotContain(n => n.Contains($"{userName}:{pw}"));
			inMemoryLogger.Lines.Should().Contain(n => n.Contains("http://[REDACTED]:[REDACTED]@localhost:8123"));
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
