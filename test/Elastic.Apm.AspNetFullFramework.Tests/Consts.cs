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
	}
}
