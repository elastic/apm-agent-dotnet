// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Model;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests;

public class TransactionExtensionTests
{
	/// <summary>
	/// Makes sure that area:null does not cause exception.
	/// See: https://github.com/elastic/apm-agent-dotnet/issues/1681
	/// </summary>
	[Fact]
	public void TestRouteWithAreaNull()
	{
		var data = new Dictionary<string, object>() { { "controller", "Home" }, { "area", null }, { "action", "Index" } };

		var ex = Record.Exception(() => Transaction.GetNameFromRouteContext(data));
		Assert.Null(ex);
	}
}
