// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.XUnit;

public sealed class FactRequiresMvcTestingFix : FactAttribute
{
	public FactRequiresMvcTestingFix()
	{
		if (Environment.Version.Major < 7)
			return;
		Skip = $"This test is disabled on .NET until https://github.com/dotnet/aspnetcore/issues/45233";
	}
}

public sealed class TheoryRequiresMvcTestingFix : TheoryAttribute
{
	public TheoryRequiresMvcTestingFix()
	{
		if (Environment.Version.Major < 7)
			return;
		Skip = $"This test is disabled on .NET until https://github.com/dotnet/aspnetcore/issues/45233";
	}
}
