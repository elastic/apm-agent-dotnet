// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Xunit;

namespace Elastic.Apm.Tests.Utilities.XUnit;

public sealed class DisabledOnWindowsTheory : TheoryAttribute
{
	public DisabledOnWindowsTheory()
	{
		if (TestEnvironment.IsWindows)
			Skip = "This test is disabled on Windows.";
	}
}
