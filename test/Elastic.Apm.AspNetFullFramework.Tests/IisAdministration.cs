using System;
using System.IO;
using System.Threading;
using Elastic.Apm.Logging;
using Microsoft.Web.Administration;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal class IisAdministration
	{
		private static readonly IisAdministration Instance = new IisAdministration();

		private IisAdministration() { }

		private static class SampleAppIisConsts
		{
			internal const string AppPoolName = "DefaultAppPool";
			internal const string SiteName = "Default Web Site";
			internal const string SrcDirPathRelativeToSolutionRoot = @"sample\AspNetFullFrameworkSampleApp";
		}

		private static class StateChangeConsts
		{
			internal const int MaxNumberOfAttemptsToVerify = 10;
			internal const int WaitBetweenVerifyAttemptsMs = 1000;
		}

		private IApmLogger _logger;

		private DirectoryInfo _solutionRoot;

		private void SetLogger(IApmLogger logger) => _logger = logger?.Scoped(nameof(IisAdministration));

		internal static void EnsureSampleAppIsRunningInCleanState(IApmLogger logger)
		{
			Instance.SetLogger(logger);
			Instance.AddSampleAppToIis();
		}

		internal static void RemoveSampleAppFromIis(IApmLogger logger)
		{
			Instance.SetLogger(logger);
			Instance.RemoveSampleAppFromIis();
		}

		private void AddSampleAppToIis()
		{
			using (var serverManager = new ServerManager())
			{
				// We need to stop/start the sample application even if application is already added because
				// we need to make sure our agent starts in a clean state and doesn't have any remains from the previous tests
				// still queued at the payload sender
				var appPool = serverManager.ApplicationPools[SampleAppIisConsts.AppPoolName];
				ChangeAppPoolStateTo(appPool, ObjectState.Stopped);

				var site = serverManager.Sites[SampleAppIisConsts.SiteName];

				var existingApp = site.Applications[Consts.SampleApp.RootUrlPath];
				if (existingApp != null) site.Applications.Remove(existingApp);

				site.Applications.Add(Consts.SampleApp.RootUrlPath,
					Path.Combine(FindSolutionRoot().FullName, SampleAppIisConsts.SrcDirPathRelativeToSolutionRoot));

				ChangeAppPoolStateTo(appPool, ObjectState.Started);

				serverManager.CommitChanges();
			}
		}

		private void RemoveSampleAppFromIis()
		{
			using (var serverManager = new ServerManager())
			{
				var appPool = serverManager.ApplicationPools[SampleAppIisConsts.AppPoolName];
				ChangeAppPoolStateTo(appPool, ObjectState.Stopped);

				var site = serverManager.Sites[SampleAppIisConsts.SiteName];
				var existingApp = site.Applications[Consts.SampleApp.RootUrlPath];
				if (existingApp != null) site.Applications.Remove(existingApp);

				ChangeAppPoolStateTo(appPool, ObjectState.Started);

				serverManager.CommitChanges();
			}
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
			if (_solutionRoot != null) return _solutionRoot;

			var solutionFileName = "ElasticApmAgent.sln";

			var currentDirectory = Directory.GetCurrentDirectory();
			var candidateDirectory = new DirectoryInfo(currentDirectory);
			do
			{
				if (File.Exists(Path.Combine(candidateDirectory.FullName, solutionFileName)))
				{
					_solutionRoot = candidateDirectory;
					return _solutionRoot;
				}

				candidateDirectory = candidateDirectory.Parent;
			} while (candidateDirectory != null);

			throw new InvalidOperationException($"Could not find solution root directory from the current directory `{currentDirectory}'");
		}
	}
}
