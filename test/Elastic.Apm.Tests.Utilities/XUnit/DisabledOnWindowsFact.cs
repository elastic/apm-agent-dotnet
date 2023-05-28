// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.InteropServices;
using Elastic.Apm.Tests.Utilities.Docker;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.XUnit;

public sealed class DisabledOnWindowsFact : FactAttribute
{
	public DisabledOnWindowsFact()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			Skip = "This test is disabled on windows";
	}
}

public sealed class DisabledOnWindowsDockerFact : DockerFactAttribute
{
	public DisabledOnWindowsDockerFact()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			Skip = "This test is disabled on windows";
	}
}

public sealed class DisabledOnWindowsTheory : TheoryAttribute
{
	public DisabledOnWindowsTheory()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			Skip = "This test is disabled on windows";
	}
}
