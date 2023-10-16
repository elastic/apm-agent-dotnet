// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.TestHelpers;
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
			using var listener = new DiagnosticListener("Test");
			var logger = new InMemoryBlockingLogger(LogLevel.Debug);
			using var agent = new ApmAgent(new TestAgentComponents(logger: logger));

			agent.Subscribe(new TestSubscriber());
			agent.Subscribe(new TestSubscriber());

			var subscribeLogs = logger.Lines.Where(line => line.StartsWith($"{{{nameof(DiagnosticInitializer)}")).ToArray();
			subscribeLogs.Should().NotBeEmpty();

			subscribeLogs.Should().Contain(line => line.Contains($"'Test' subscribed by: {nameof(TestListener)}"));
			subscribeLogs.Should().Contain(line => line.Contains($"'Test' already subscribed by: {nameof(TestListener)}"));
			agent.SubscribedListeners().Should().HaveCount(1).And.Contain(typeof(TestListener));
		}

		[Fact]
		public void DisposeSubscriptionShouldRemoveFromSubscribedListeners()
		{
			using var listener = new DiagnosticListener("Test");
			var logger = new InMemoryBlockingLogger(LogLevel.Debug);
			using var agent = new ApmAgent(new TestAgentComponents(logger: logger));

			using (agent.Subscribe(new TestSubscriber()))
			{
				var subscribeLogs = logger.Lines.Where(line => line.StartsWith($"{{{nameof(DiagnosticInitializer)}")).ToArray();
				subscribeLogs.Should().NotBeEmpty();
				subscribeLogs.Should().Contain(line => line.Contains($"'Test' subscribed by: {nameof(TestListener)}"));
				agent.SubscribedListeners().Should().HaveCount(1).And.Contain(typeof(TestListener));
			}

			agent.SubscribedListeners().Should().BeEmpty();
		}
	}
}
