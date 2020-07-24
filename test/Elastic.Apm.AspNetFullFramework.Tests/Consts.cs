// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal static class Consts
	{
		internal static class SampleApp
		{
			internal const string AppName = "Elastic.Apm.AspNetFullFramework.Tests.SampleApp";
			internal const string AppPoolName = AppName + "Pool";
			internal const string Host = "localhost";
			internal const string RootUrl = "http://" + Host + RootUrlPath;
			internal const string RootUrlPath = "/" + AppName;
			internal const string SiteName = "Default Web Site";
			internal const string SrcDirPathRelativeToSolutionRoot = @"sample\AspNetFullFrameworkSampleApp";
		}

		internal const string AspNetFullFrameworkTestsCollection = "AspNetFullFrameworkTests";
	}
}
