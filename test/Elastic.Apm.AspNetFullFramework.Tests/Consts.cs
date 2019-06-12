using System;
using System.IO;
using Microsoft.Web.Administration;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal static class Consts
	{
		internal static class SampleApp
		{
			internal const string rootUrlPath = "/Elastic.Apm.AspNetFullFramework.Tests.SampleApp";
			internal const string rootUri = "http://localhost" + rootUrlPath;
			internal const string homePageRelativePath = "Home";
			internal const string contactPageRelativePath = "Home/Contact";
		}
	}
}
