// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests;

public class TransactionQueueTests
{
	/// <summary>
	/// Makes sure <see cref="AspNetCoreDiagnosticListener"/> does not keep references to GCed <see cref="HttpContext"/> instances.
	/// </summary>
	[Fact]
	public void WeakReferenceTest()
	{
		using var agent = new ApmAgent(new TestAgentComponents());
		var listener = new AspNetCoreDiagnosticListener(agent);

		AddItem(listener);

		listener.ProcessingRequests.Count().Should().Be(1);
		GC.Collect();
		Thread.Sleep(10);
		GC.Collect();
		foreach (var item in listener.ProcessingRequests) { }

		listener.ProcessingRequests.Count().Should().Be(0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void AddItem(AspNetCoreDiagnosticListener listener) =>
		listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start", new DefaultHttpContext()));
}
