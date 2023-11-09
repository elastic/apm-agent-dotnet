// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Metrics;

public class FreeAndTotalMemoryProviderTests
{
	[Fact]
	public void FreeAndTotalMemoryProvider_ShouldReturnValues()
	{
		var sut = new FreeAndTotalMemoryProvider(new NoopLogger(), new List<WildcardMatcher>());

		var samples = sut.GetSamples().ToList();
		samples.First().Samples.Should().HaveCount(2);

		var freeMemory = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == FreeAndTotalMemoryProvider.FreeMemory);
		freeMemory.Should().NotBeNull();
		freeMemory.KeyValue.Value.Should().BeGreaterThan(0);

		var totalMemory = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == FreeAndTotalMemoryProvider.TotalMemory);
		totalMemory.Should().NotBeNull();
		totalMemory.KeyValue.Value.Should().BeGreaterThan(0);
	}
}
