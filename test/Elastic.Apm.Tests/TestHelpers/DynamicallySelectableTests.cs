using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class DynamicallySelectableTests : LoggingTestBase
	{
		public DynamicallySelectableTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[Fact]
		public void Names_consts_should_be_correct()
		{
			DynamicallySelectableTestCaseDiscoverer.ThisClassFullName.Should().Be(typeof(DynamicallySelectableTestCaseDiscoverer).FullName);

			// "Elastic.Apm.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22"
			typeof(DynamicallySelectableTestCaseDiscoverer).Assembly.FullName.Should()
				.StartWith(
					$"{DynamicallySelectableTestCaseDiscoverer.ThisClassAssemblyName}, Version=");
		}

		[AlwaysSelectedFact]
		public void AlwaysSelectedFact_test()
		{
			true.Should().BeTrue();
		}

		[NeverSelectedFact]
		public void NeverSelectedFact_test()
		{
			false.Should().BeTrue();
		}

		public class AlwaysSelectedFactAttribute : DynamicallySelectableFactAttribute
		{
			public override string ReasonNotSelected => null;
		}

		public class NeverSelectedFactAttribute : DynamicallySelectableFactAttribute
		{
			public override string ReasonNotSelected => "[NeverSelectedFact] is a dummy attribute that doesn't select any tests.";
		}
	}
}
