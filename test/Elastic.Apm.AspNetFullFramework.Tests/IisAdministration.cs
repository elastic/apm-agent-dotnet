using System;
using System.IO;
using Microsoft.Web.Administration;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal static class IisAdministration
	{
		private static bool triedToAddSampleApp = false;
		private static bool sampleAppAdded = false;

		private static class SampleAppIisConsts
		{
			internal const string siteName = "Default Web Site";
			internal const string srcDirPathRelativeToSolutionRoot = @"sample\AspNetFullFrameworkSampleApp";
		}

		internal static void AddSampleAppToIis()
		{
			if (triedToAddSampleApp)
			{
				if (sampleAppAdded)
					return;

				throw new InvalidOperationException($"Already tried to add Sample IIS application once but failed");
			}

			using (var serverManager = new ServerManager())
			{
				var site = serverManager.Sites[SampleAppIisConsts.siteName];

				var existingApp = site.Applications[Consts.SampleApp.rootUrlPath];
				if (existingApp != null) site.Applications.Remove(existingApp);

				var app = site.Applications.Add(Consts.SampleApp.rootUrlPath,
					Path.Combine(FindSolutionRoot().FullName, SampleAppIisConsts.srcDirPathRelativeToSolutionRoot));

				serverManager.CommitChanges();
			}

			sampleAppAdded = true;
		}

		private static DirectoryInfo FindSolutionRoot()
		{
			var solutionFileName = "ElasticApmAgent.sln";

			var currentDirectory = Directory.GetCurrentDirectory();
			var candidateDirectory = new DirectoryInfo(currentDirectory);
			do
			{
				if (File.Exists(Path.Combine(candidateDirectory.FullName, solutionFileName))) return candidateDirectory;

				candidateDirectory = candidateDirectory.Parent;
			} while (candidateDirectory != null);

			throw new InvalidOperationException($"Could not find solution root directory from the current directory `{currentDirectory}'");
		}
	}
}
