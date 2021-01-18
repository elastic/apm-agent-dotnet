using FluentAssertions;
using Xunit;

namespace Elastic.Apm.StaticExplicitInitialization.Tests
{
	public class StaticImplicitInitializationTests
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
