using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class MetricsWithAccessToPerfCountersTests : MetricsTestsBase
	{
		public MetricsWithAccessToPerfCountersTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper, /* sampleAppShouldHaveAccessToPerfCounters: */ true) { }

		//[AspNetFullFrameworkFact]
		//public async Task VerifyMetricsBasicConstraints() => await VerifyMetricsBasicConstraintsImpl();
	}
}
