using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class DynamicallySelectableTestCaseDiscoverer : FactDiscoverer
	{
		public const string ThisClassAssemblyName = "Elastic.Apm.Tests";
		public const string ThisClassFullName = "Elastic.Apm.Tests.TestHelpers." + ThisClassName;
		public const string ThisClassName = nameof(DynamicallySelectableTestCaseDiscoverer);

		internal static readonly bool NotSelectedIsSkipped = TestingConfig.ReadFromFromEnvVars().NotSelectedIsSkipped;

		internal static readonly IApmLogger LoggerBase = new LineWriterToLoggerAdaptor(
			new SystemDiagnosticsTraceLineWriter( /* prefix: */ "Elastic APM .NET Tests Discovery> ")
			, TestingConfig.ReadFromFromEnvVars().LogLevelForDiscovery);


		private readonly IApmLogger _logger;

		public DynamicallySelectableTestCaseDiscoverer(IMessageSink messageSink)
			: base(messageSink)
		{
			_logger = LoggerBase.Scoped(ThisClassName);
		}

		public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod
			, IAttributeInfo factAttribute
		) => ExceptionUtils.DoSwallowingExceptionsWithResult(_logger, () => DiscoverImpl(discoveryOptions, testMethod, factAttribute));

		private  IEnumerable<IXunitTestCase> DiscoverImpl(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod
			, IAttributeInfo factAttribute
		)
		{
			_logger.Trace()?.Log("Test method: {TestClassName}.{TestMethodName}", testMethod.TestClass.Class.Name, testMethod.Method.Name);

			var attributes = testMethod.Method.GetCustomAttributes(typeof(DynamicallySelectableFactAttribute).AssemblyQualifiedName);

			// ReSharper disable PossibleMultipleEnumeration
			_logger.Trace()?.Log("attributes [size: {size}]: {attributes}", attributes.Count(), string.Join(", ", attributes));

			if (attributes.Count() != 1)
				throw new InvalidOperationException($"Method should have exactly one {nameof(DynamicallySelectableFactAttribute)}");

			var reasonNotSelected = attributes.First().GetNamedArgument<string>(nameof(DynamicallySelectableFactAttribute.ReasonNotSelected));
			_logger.Trace()?.Log("ReasonNotSelected: {ReasonNotSelected}", reasonNotSelected.AsNullableToString());
			// ReSharper restore PossibleMultipleEnumeration

			return reasonNotSelected == null || NotSelectedIsSkipped
				? base.Discover(discoveryOptions, testMethod, factAttribute)
				: new IXunitTestCase[]
				{
					new NotSelectedTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, reasonNotSelected)
				};

		}

		public class NotSelectedTestCase : XunitTestCase
		{
			[EditorBrowsable(EditorBrowsableState.Never)]
			[Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
			// ReSharper disable once UnusedMember.Global
			public NotSelectedTestCase() { }

			public NotSelectedTestCase(IMessageSink diagnosticMessageSink
				, TestMethodDisplay defaultMethodDisplay
				, ITestMethod testMethod
				, string reasonNotSelected
			)
				: base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
			{
				ReasonNotSelected = reasonNotSelected;
			}

			public string ReasonNotSelected { get; set; }

			/// <inheritdoc />
			public override void Serialize(IXunitSerializationInfo data)
			{
				base.Serialize(data);
				data.AddValue(nameof(ReasonNotSelected), ReasonNotSelected);
			}

			/// <inheritdoc />
			public override void Deserialize(IXunitSerializationInfo data)
			{
				base.Deserialize(data);
				ReasonNotSelected = data.GetValue<string>(nameof(ReasonNotSelected));
			}

			/// <inheritdoc />
			public override Task<RunSummary> RunAsync(
				IMessageSink diagnosticMessageSink,
				IMessageBus messageBus,
				object[] constructorArguments,
				ExceptionAggregator aggregator,
				CancellationTokenSource cancellationTokenSource
			)
			{
				return new NotSelectedTestCaseRunner(this, messageBus, aggregator, cancellationTokenSource).RunAsync();
			}
		}

		public class NotSelectedTestCaseRunner : TestCaseRunner<NotSelectedTestCase>
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private const string ThisClassName = DynamicallySelectableTestCaseDiscoverer.ThisClassName + "." + nameof(NotSelectedTestCaseRunner);

			private readonly IApmLogger _logger;

			private readonly NotSelectedTestCase _testCase;

			public NotSelectedTestCaseRunner(NotSelectedTestCase testCase
				, IMessageBus messageBus
				, ExceptionAggregator aggregator
				, CancellationTokenSource cancellationTokenSource
			)
				: base(testCase, messageBus, aggregator, cancellationTokenSource)
			{
				_logger = LoggerBase.Scoped(ThisClassName);
				_testCase = testCase;
			}

			/// <inheritdoc />
			protected override Task<RunSummary> RunTestAsync()
			{
				_logger.Trace()
					?.Log("Simulating test case execution..."
						+ " TestCaseDisplayName: {TestCaseDisplayName}."
						+ " ReasonNotSelected: {ReasonNotSelected}."
						, _testCase.DisplayName, _testCase.ReasonNotSelected);

				var xunitTest = new XunitTest(TestCase, TestCase.DisplayName);
				var result = new RunSummary { Total = 1 };
				if (BroadcastTestStatus(new TestStarting(xunitTest)))
				{
					BroadcastTestStatus(new TestPassed(xunitTest, /* executionTime: */ decimal.Zero,
						$"Test is not executed because it is not selected. Reason for not being selected: {_testCase.ReasonNotSelected}"));
				}
				BroadcastTestStatus(new TestFinished(xunitTest, /* executionTime: */ decimal.Zero, /* output: */ null));
				return Task.FromResult(result);

				bool BroadcastTestStatus(IMessageSinkMessage testStatusMessage)
				{
					_logger.Trace()?.Log("Queueing test status message... TestStatusMessage: {TestStatusMessage}.", testStatusMessage);
					var shouldExecutionContinue = MessageBus.QueueMessage(testStatusMessage);
					if (!shouldExecutionContinue)
						CancellationTokenSource.Cancel();
					_logger.Trace()?.Log("Queued test status message. ShouldExecutionContinue: {ShouldExecutionContinue}", shouldExecutionContinue);
					return shouldExecutionContinue;
				}
			}
		}
	}
}
