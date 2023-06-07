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

public sealed class DisabledOnFullFrameworkFact : FactAttribute
{
	public DisabledOnFullFrameworkFact()
	{
#if NETFRAMEWORK
		Skip = "This test is disabled on .NET Full Framework";
#endif
	}
}

public sealed class DisabledOnWindowsCIDockerFact : DockerFactAttribute
{
	public DisabledOnWindowsCIDockerFact()
	{
		if (TestEnvironment.IsCi && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			Skip = "This test is disabled on windows CI";
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

public sealed class DisabledOnFullFrameworkTheory : TheoryAttribute
{
	public DisabledOnFullFrameworkTheory()
	{
#if NETFRAMEWORK
		Skip = "This test is disabled on .NET Full Framework";
#endif
	}
}
