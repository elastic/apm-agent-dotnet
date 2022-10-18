// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;
using Xunit;

namespace Elastic.Apm.StaticImplicitInitialization.Tests
{
	/// <summary>
	/// These tests access the static <see cref="Agent"/> instance.
	/// All other tests should have their own <see cref="ApmAgent"/> instance and not rely on anything static.
	/// Tests accessing the static <see cref="Agent"/> instance cannot run in parallel with tests that also access the static instance.
	/// </summary>
	public class ImplicitInitializationTests
	{
		/// <summary>
		/// Makes sure Agent.IsConfigured is true after implicit agent initialization
		/// </summary>
		[Fact]
		public void IsConfiguredWithImplicitInitialization()
		{
			Agent.IsConfigured.Should().BeFalse();

			Agent.Tracer.CaptureTransaction("Foo", "Bar", () => { });
			Agent.IsConfigured.Should().BeTrue();
		}
	}
}
