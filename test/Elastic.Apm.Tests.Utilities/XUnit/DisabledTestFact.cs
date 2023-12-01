// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.XUnit;

public sealed class DisabledTestFact : FactAttribute
{
	public DisabledTestFact(string reason, string issueLink = null)
	{
		Skip = $"This Test is disabled, with reason: {reason}";
		if (!string.IsNullOrEmpty(issueLink))
			Skip += $", issue link: {issueLink}";
	}
}

public sealed class FactRequiresMvcTestingFix : FactAttribute
{
	public FactRequiresMvcTestingFix()
	{
		if (Environment.Version.Major < 7) return;
		Skip = $"This Test is disabled on .NET 7 until https://github.com/dotnet/aspnetcore/issues/45233";
	}
}

