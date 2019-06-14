using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class MetricsWithoutAccessToPerfCountersTests : MetricsTestsBase
	{
		public MetricsWithoutAccessToPerfCountersTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper, /* sampleAppShouldHaveAccessToPerfCounters: */ false) { }

		[AspNetFullFrameworkFact]
		public async Task VerifyPeriodicallySentMetrics() => await VerifyPeriodicallySentMetricsImpl();
	}
}
