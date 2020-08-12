// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
