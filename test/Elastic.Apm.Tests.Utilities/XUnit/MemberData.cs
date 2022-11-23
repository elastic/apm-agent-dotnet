// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System;
using System.Collections.Generic;

namespace Elastic.Apm.Tests.Utilities.XUnit;

public static class MemberData
{
	public static IEnumerable<object[]> TestWithDiagnosticSourceOnly()
	{
		yield return new object[] { false };
		//
		// Skip "DiagnosticSourceOnly" tests on .NET 7
		// until https://github.com/dotnet/aspnetcore/issues/45233 is resolved.
		//
		if (Environment.Version.Major < 7)
			yield return new object[] { true };
	}

}
