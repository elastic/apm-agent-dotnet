using System;
using Elastic.Apm.Helpers;
using Xunit;
// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

internal static class LazyContextualInitTestsHelpers
{
	public static TheoryData<string, Func<LazyContextualInit, Action, bool>> WaysToCallInit = new()
		{
			{ "IfNotInited?.Init ?? false", (lazyCtxInit, initAction) => lazyCtxInit.IfNotInited?.Init(initAction) ?? false },
			{ "Init", (lazyCtxInit, initAction) => lazyCtxInit.Init(initAction) }
		};
}