// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Tests.Utilities.Docker;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.XUnit;

#pragma warning disable IDE0021 // Use expression body for constructor
public sealed class DisabledOnWindowsFact : FactAttribute
{
	public DisabledOnWindowsFact()
	{
		if (TestEnvironment.IsWindows)
			Skip = "This test is disabled on Windows.";
	}
}

public sealed class DisabledOnNet462FrameworkFact : FactAttribute
{
	public DisabledOnNet462FrameworkFact()
	{
#if NET462
		Skip = "This test is disabled on .NET Framework 4.6.2";
#endif
	}
}


#pragma warning restore IDE0021 // Use expression body for constructor
