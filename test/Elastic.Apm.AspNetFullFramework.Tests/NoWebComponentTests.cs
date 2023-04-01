using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests;

public class NoWebComponentTests
{
	[Fact]

	public void RunningFrameworkTestsButNoWebComponentsDetected()
	{

		false.Should().BeTrue("Attempting to run IIS tests but no visual build tools are detected");
	}

}
