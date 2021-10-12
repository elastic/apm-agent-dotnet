// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.DiagnosticSource
{
	public class DiagnosticListenerTests
	{
		internal class TestSubscriber : IDiagnosticsSubscriber
		{
			public IDisposable Subscribe(IApmAgent agent)
			{
				var retVal = new CompositeDisposable();

				var initializer = new DiagnosticInitializer(agent, new TestListener(agent));

				retVal.Add(initializer);
				retVal.Add(DiagnosticListener
					.AllListeners
					.Subscribe(initializer));

				return retVal;
			}
		}

		internal class TestListener : DiagnosticListenerBase
		{
			public TestListener(IApmAgent apmAgent) : base(apmAgent) { }

			protected override void HandleOnNext(KeyValuePair<string, object> kv)
			{
			}

			public override string Name { get; } = "Test";
		}

		[Fact]
		public void SubscribeDuplicateDiagnosticListenerTypesShouldOnlySubscribeSingle()
		{
			using (var listener = new DiagnosticListener("Test"))
			{
				var logger = new InMemoryBlockingLogger(LogLevel.Debug);
				using var agent = new ApmAgent(new TestAgentComponents(logger: logger));

				agent.Subscribe(new TestSubscriber());
				agent.Subscribe(new TestSubscriber());

				logger.Lines.Should().Contain(line => line.Contains($"Subscribed {typeof(TestListener).FullName} to `Test' events source"));
				logger.Lines.Should().Contain(line => line.Contains($"already subscribed to `Test' events source"));
				agent.SubscribedListeners.Should().HaveCount(1).And.Contain(typeof(TestListener));
			}
		}
	}
}
