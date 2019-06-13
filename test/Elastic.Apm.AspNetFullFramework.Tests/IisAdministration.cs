using System;
using System.IO;
using System.Threading;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.Web.Administration;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal class IisAdministration
	{
		private readonly IApmLogger _logger;

		internal IisAdministration(IApmLogger logger) => _logger = logger?.Scoped(nameof(IisAdministration));

		private static class SampleAppIisConsts
		{
			internal const string AppPoolName = "Elastic.Apm.AspNetFullFramework.Tests.SampleAppPool";
			internal const string SiteName = "Default Web Site";
			internal const string SrcDirPathRelativeToSolutionRoot = @"sample\AspNetFullFrameworkSampleApp";
		}

		private static class StateChangeConsts
		{
			internal const int MaxNumberOfAttemptsToVerify = 10;
			internal const int WaitBetweenVerifyAttemptsMs = 1000;
		}

		internal void SetupSampleAppInCleanState(int apmServerPort)
		{
			using (var serverManager = new ServerManager())
			{
				// We need to stop/start the sample application to make sure our agent starts in a clean state and
				// doesn't have any remains from the previous tests still queued by the payload sender part of the agent.
				serverManager.ApplicationPools[SampleAppIisConsts.AppPoolName]?.Let(appPool => ChangeAppPoolStateTo(appPool, ObjectState.Stopped));

				AddSampleAppPool(serverManager, apmServerPort);
				AddSampleApp(serverManager);

				serverManager.CommitChanges();

				// Since we just removed and then re-added application pool we need commit changes first
				// and only then we can start the application pool.
				ChangeAppPoolStateTo(serverManager.ApplicationPools[SampleAppIisConsts.AppPoolName], ObjectState.Started);
			}
		}

		internal void StartSampleAppPool(int apmServerPort)
		{
			using (var serverManager = new ServerManager())
			{
			}
		}

		internal void DisposeSampleApp()
		{
			using (var serverManager = new ServerManager())
			{
				var existingAppPool = serverManager.ApplicationPools[SampleAppIisConsts.AppPoolName];
				if (existingAppPool != null) ChangeAppPoolStateTo(existingAppPool, ObjectState.Stopped);

				var site = serverManager.Sites[SampleAppIisConsts.SiteName];
				var existingApp = site.Applications[Consts.SampleApp.RootUrlPath];
				if (existingApp != null) site.Applications.Remove(existingApp);

				if (existingAppPool != null) serverManager.ApplicationPools.Remove(existingAppPool);

				serverManager.CommitChanges();
			}
		}

		private void AddSampleAppPool(ServerManager serverManager, int apmServerPort)
		{
			var existingAppPool = serverManager.ApplicationPools[SampleAppIisConsts.AppPoolName];
			if (existingAppPool != null) serverManager.ApplicationPools.Remove(existingAppPool);
			var addedPool = serverManager.ApplicationPools.Add(SampleAppIisConsts.AppPoolName);
			addedPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
			addedPool.StartMode = StartMode.OnDemand;

			AddEnvVarsForSampleAppPool(serverManager, apmServerPort);
		}

		private void AddSampleApp(ServerManager serverManager)
		{
			var site = serverManager.Sites[SampleAppIisConsts.SiteName];

			var existingApp = site.Applications[Consts.SampleApp.RootUrlPath];
			if (existingApp != null) site.Applications.Remove(existingApp);

			var addedApp = site.Applications.Add(Consts.SampleApp.RootUrlPath,
				Path.Combine(FindSolutionRoot().FullName, SampleAppIisConsts.SrcDirPathRelativeToSolutionRoot));

			addedApp.ApplicationPoolName = SampleAppIisConsts.AppPoolName;
		}

		private void ChangeAppPoolStateTo(ApplicationPool appPool, ObjectState targetState)
		{
			ObjectState ingState;
			Action<ApplicationPool> changeAction;
			if (targetState == ObjectState.Stopped)
			{
				ingState = ObjectState.Stopping;
				changeAction = ap => ap.Stop();
			}
			else
			{
				ingState = ObjectState.Starting;
				changeAction = ap => ap.Start();
			}

			var currentState = appPool.State;
			if (currentState == ingState || currentState == targetState)
				_logger.Debug()?.Log("IIS application pool `{IisAppPool}' is already in {IisAppPoolState} state...", appPool.Name, currentState);
			else
			{
				_logger.Debug()?.Log("{StateChanging} IIS application pool `{IisAppPool}'...", ingState, appPool.Name);
				changeAction(appPool);
			}

			var attemptNumber = 0;
			while (true)
			{
				++attemptNumber;
				currentState = appPool.State;
				if (currentState == targetState)
				{
					_logger.Debug()
						?.Log("IIS application pool `{IisAppPool}' changed to target state {IisAppPoolState}. " +
							"Attempt to verify #{AttemptNumber} out of {MaxNumberOfAttempts}",
							appPool.Name, targetState,
							attemptNumber, StateChangeConsts.MaxNumberOfAttemptsToVerify);
					return;
				}

				if (attemptNumber == StateChangeConsts.MaxNumberOfAttemptsToVerify)
				{
					var ex = new InvalidOperationException($"Could not change IIS application pool (`{appPool.Name}') state to {targetState}. " +
						$"Last seen state: {currentState}");
					_logger.Error()
						?.LogException(ex,
							"Reached max number of attempts to verify if application pool changed to target state - throwing an exception...");
					throw ex;
				}

				_logger.Debug()
					?.Log("IIS application pool `{IisAppPool}' still didn't change to target state {IisAppPoolState}. " +
						"Attempt to verify #{AttemptNumber} out of {MaxNumberOfAttempts}. " +
						"Waiting {WaitTimeMs}ms before the next attempt...",
						appPool.Name, targetState,
						attemptNumber, StateChangeConsts.MaxNumberOfAttemptsToVerify,
						StateChangeConsts.WaitBetweenVerifyAttemptsMs);
				Thread.Sleep(StateChangeConsts.WaitBetweenVerifyAttemptsMs);
			}
		}

		private DirectoryInfo FindSolutionRoot()
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

		private void AddEnvVarsForSampleAppPool(ServerManager serverManager, int apmServerPort)
		{
			Configuration config = serverManager.GetApplicationHostConfiguration();
			ConfigurationSection appPoolsSection = config.GetSection("system.applicationHost/applicationPools");
			ConfigurationElementCollection appPoolsCollection = appPoolsSection.GetCollection();
			ConfigurationElement sampleAppPoolAddElement = FindConfigurationElement(appPoolsCollection, "add", "name", SampleAppIisConsts.AppPoolName);
			if (sampleAppPoolAddElement == null)
				throw new InvalidOperationException($"Element <add> for application pool {SampleAppIisConsts.AppPoolName} not found");
			ConfigurationElementCollection envVarsCollection = sampleAppPoolAddElement.GetCollection("environmentVariables");
			ConfigurationElement envVarAddElement = envVarsCollection.CreateElement("add");
			envVarAddElement["name"] = ConfigConsts.EnvVarNames.ServerUrls;
			envVarAddElement["value"] = $"http://localhost:{apmServerPort}";
			envVarsCollection.Add(envVarAddElement);
		}

		private static ConfigurationElement FindConfigurationElement(ConfigurationElementCollection collection, string elementTagName, params string[] keyValues)
		{
			foreach (ConfigurationElement element in collection)
			{
				if (String.Equals(element.ElementTagName, elementTagName, StringComparison.OrdinalIgnoreCase))
				{
					bool matches = true;
					for (int i = 0; i < keyValues.Length; i += 2)
					{
						object o = element.GetAttributeValue(keyValues[i]);
						string value = null;
						if (o != null)
						{
							value = o.ToString();
						}
						if (!String.Equals(value, keyValues[i + 1], StringComparison.OrdinalIgnoreCase))
						{
							matches = false;
							break;
						}
					}
					if (matches)
					{
						return element;
					}
				}
			}
			return null;
		}
	}
}
