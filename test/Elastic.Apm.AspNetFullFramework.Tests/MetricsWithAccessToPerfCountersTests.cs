using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class MetricsWithAccessToPerfCountersTests : MetricsTestsBase
	{
		public MetricsWithAccessToPerfCountersTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper, /* sampleAppShouldHaveAccessToPerfCounters: */ true) { }

		[AspNetFullFrameworkFact]
		public async Task VerifyPeriodicallySentMetrics() => await VerifyPeriodicallySentMetricsImpl();
	}
}
