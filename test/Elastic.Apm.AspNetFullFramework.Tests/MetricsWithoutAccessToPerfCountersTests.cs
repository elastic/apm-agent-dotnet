using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class MetricsWithoutAccessToPerfCountersTests : MetricsTestsBase
	{
		public MetricsWithoutAccessToPerfCountersTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper, /* sampleAppShouldHaveAccessToPerfCounters: */ false) { }

		[AspNetFullFrameworkFact]
		public async Task VerifyMetricsBasicConstraints() => await VerifyMetricsBasicConstraintsImpl();
	}
}
