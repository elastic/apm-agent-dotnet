// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Extensions.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Extensions.Logging.Tests;

public class CaptureApmErrorsTests
{
	private readonly ExtensionsTestHelper _extensionsTestHelper;

	public CaptureApmErrorsTests(ITestOutputHelper output)
	{
		_extensionsTestHelper = new(output);
		_extensionsTestHelper.TestSetup();
	}

	[Fact]
	public async Task UseElasticApm_CaptureErrorLogsAsApmError() =>
		await _extensionsTestHelper.ExecuteTestProcessAsync(null, false, true, false, true);

	[Fact]
	public async Task AddElasticApm_CaptureErrorLogsAsApmError() =>
		await _extensionsTestHelper.ExecuteTestProcessAsync(null, false, false, false, true);
}
