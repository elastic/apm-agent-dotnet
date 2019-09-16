using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.Web.Administration;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal class IisAdministration
	{
		private readonly IApmLogger _logger;

		internal IisAdministration(IApmLogger logger) => _logger = logger?.Scoped(nameof(IisAdministration));

		private static class StateChangeConsts
		{
			internal const int LogMessageAfterNInitialAttempts = 30; // i.e., log the first message after 3 seconds (if it's still failing)
			internal const int LogMessageEveryNAttempts = 10; // i.e., log message every second (if it's still failing)
			internal const int MaxNumberOfAttemptsToVerify = 600; // 1 minute (60 seconds) max wait time (600 * 100ms)
			internal const int WaitBetweenVerifyAttemptsMs = 100;
		}

		internal void SetupSampleAppInCleanState(Dictionary<string, string> envVarsToSetForSampleAppPool,
			bool sampleAppShouldUseHighPrivilegedAccount
		)
		{
			// We need to stop/start the sample application to make sure our agent starts in a clean state and
			// doesn't have any remains from the previous tests still queued by the payload sender part of the agent.

			_logger.Debug()?.Log("IIS applicationHost config before stopping and removing application pool:");
			LogIisApplicationHostConfig();

			using (var serverManager = new ServerManager())
			{
//				AddSampleAppPool(serverManager, envVarsToSetForSampleAppPool, sampleAppShouldUseHighPrivilegedAccount, StartMode.AlwaysRunning);
				AddSampleAppPool(serverManager, envVarsToSetForSampleAppPool, sampleAppShouldUseHighPrivilegedAccount, StartMode.OnDemand);
				AddSampleApp(serverManager);

				serverManager.CommitChanges();
			}

			_logger.Debug()?.Log("IIS applicationHost config after removing and adding application pool:");
			LogIisApplicationHostConfig();

			// We need stop/start sample application pool to make sure there are no worker processes still running
			// with old application pool configuration settings (especially environment variables)

			LogAboutWorkerProcesses("Before stopping application pool");

			using (var serverManager = new ServerManager())
			{
				StopSampleAppPool(serverManager);
				serverManager.CommitChanges();
			}

			using (var serverManager = new ServerManager())
			{
				ChangeAppPoolStateTo(serverManager.ApplicationPools[Consts.SampleApp.AppPoolName], ObjectState.Started);
				serverManager.CommitChanges();
			}

			LogAboutWorkerProcesses("After starting application pool");

			// It's not entirely clear why but it seems that application pool configuration sometimes does not take after one restart
			// (That is newly started worker process still uses old configuration, in particular environment variables).
			// This issue is not reproducible on a laptop with Windows 10
			// but it's easily reproducible in a docker container running on the same laptop.
			// Restarting application pool seems to fix the issue we can use this hack unless it will make tests flaky.
			// We have assert in unit tests verifying that IIS worker process has expected environment variables
			// so if the issue occurs even with this hack it will be detected.

			_logger.Debug()?.Log("Stopping and the starting application pool 2nd time");

			LogAboutWorkerProcesses("Before stopping application pool");

			using (var serverManager = new ServerManager())
			{
				StopSampleAppPool(serverManager);
				serverManager.CommitChanges();
			}

			using (var serverManager = new ServerManager())
			{
				ChangeAppPoolStateTo(serverManager.ApplicationPools[Consts.SampleApp.AppPoolName], ObjectState.Started);
				serverManager.CommitChanges();
			}

			LogAboutWorkerProcesses("After starting application pool");
		}

		private void LogAboutWorkerProcesses(string prefix)
		{
			using (var serverManager = new ServerManager())
			{
				var workerProcessesPids = GetWorkerProcessesPids(serverManager);
				_logger.Debug()
					?.Log(prefix + " found {NumberOfWorkerProcesses} worker processes." +
						" Their PIDs: {WorkerProcessesPIDs}", workerProcessesPids.Count, string.Join(", ", workerProcessesPids));
			}
		}

		private static List<int> GetWorkerProcessesPids(ServerManager serverManager) =>
			serverManager.WorkerProcesses.Where(wp => wp.AppPoolName == Consts.SampleApp.AppPoolName).Select(wp => wp.ProcessId).ToList();

		private void StopSampleAppPool(ServerManager serverManager)
		{
			serverManager.ApplicationPools[Consts.SampleApp.AppPoolName]?.Let(appPool => ChangeAppPoolStateTo(appPool, ObjectState.Stopped));

			EnsureNoRunningWorkerProcesses(serverManager);
		}

		private void EnsureNoRunningWorkerProcesses(ServerManager serverManager)
		{
			var workerProcessesPids = serverManager.WorkerProcesses.Where(wp => wp.AppPoolName == Consts.SampleApp.AppPoolName)
				.Select(wp => wp.ProcessId)
				.ToList();
			_logger.Debug()
				?.Log("After stopping application pool found {NumberOfWorkerProcesses} worker processes." +
					" Their PIDs: {WorkerProcessesPIDs}", workerProcessesPids.Count, string.Join(", ", workerProcessesPids));

			foreach (var workerProcessPid in workerProcessesPids)
			{
				Process workerProcess;
				try
				{
					workerProcess = Process.GetProcessById(workerProcessPid);
				}
				catch (ArgumentException ex)
				{
					_logger.Debug()?.LogException(ex, "Worker process with PID {WorkerProcessPID} has already exited", workerProcessPid);
					continue;
				}

				using (workerProcess)
				{
					_logger.Info()?.Log("About to kill worker process with PID {WorkerProcessPID}...", workerProcessPid);
					try
					{
						workerProcess.Kill();
					}
					catch (Exception ex)
					{
						_logger.Debug()
							?.LogException(ex, "Attempt to kill worker process with PID {WorkerProcessPID} has thrown exception" +
								" - maybe process has already exited?", workerProcessPid);
					}
					const int maxWaitMs = 10 * 1000; // 10 seconds
					_logger.Info()
						?.Log("Waiting for worker process with PID {WorkerProcessPID} to exit... Max wait time: {MaxWaitTimeMs}ms",
							workerProcessPid, maxWaitMs);
					var hasProcessExited = workerProcess.WaitForExit(maxWaitMs);
					if (hasProcessExited)
						_logger.Info()?.Log("Worker process with PID {WorkerProcessPID} has exited", workerProcessPid);
					else
					{
						_logger.Error()?.Log("Worker process with PID {WorkerProcessPID} has NOT exited during wait", workerProcessPid);
						throw new InvalidOperationException($"Failed to stop IIS worker process with PID {workerProcessPid}");
					}
				}
			}
		}

		internal void DisposeSampleApp()
		{
			using (var serverManager = new ServerManager())
			{
				var existingAppPool = serverManager.ApplicationPools[Consts.SampleApp.AppPoolName];
				if (existingAppPool != null) ChangeAppPoolStateTo(existingAppPool, ObjectState.Stopped);

				var site = serverManager.Sites[Consts.SampleApp.SiteName];
				var existingApp = site.Applications[Consts.SampleApp.RootUrlPath];
				if (existingApp != null)
				{
					site.Applications.Remove(existingApp);
					_logger.Debug()?.Log("Removed application {IisApp}", Consts.SampleApp.RootUrlPath);
				}
				else
					_logger.Debug()?.Log("No need to remove application {IisApp} - it doesn't exist", Consts.SampleApp.RootUrlPath);

				if (existingAppPool != null)
				{
					serverManager.ApplicationPools.Remove(existingAppPool);
					_logger.Debug()?.Log("Removed application pool {IisAppPool}", Consts.SampleApp.AppPoolName);
				}
				else
					_logger.Debug()?.Log("No need to remove application pool {IisAppPool} - it doesn't exist", Consts.SampleApp.AppPoolName);

				serverManager.CommitChanges();
			}

			using (var serverManager = new ServerManager())
			{
				StopSampleAppPool(serverManager);
				serverManager.CommitChanges();
			}
		}

		private void AddSampleAppPool(ServerManager serverManager,
			Dictionary<string, string> envVarsToSetForSampleAppPool,
			bool sampleAppShouldUseHighPrivilegedAccount,
			StartMode startMode
		)
		{
			_logger.Debug()
				?.Log("Adding application pool {IisAppPool}, useHighPrivilegedAccount: {useHighPrivilegedAccount}...",
					Consts.SampleApp.AppPoolName, sampleAppShouldUseHighPrivilegedAccount);
			var existingAppPool = serverManager.ApplicationPools[Consts.SampleApp.AppPoolName];
			if (existingAppPool != null) serverManager.ApplicationPools.Remove(existingAppPool);
			var addedPool = serverManager.ApplicationPools.Add(Consts.SampleApp.AppPoolName);
			addedPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
			addedPool.StartMode = startMode;
			if (sampleAppShouldUseHighPrivilegedAccount) addedPool.ProcessModel.IdentityType = ProcessModelIdentityType.LocalSystem;
			_logger.Debug()
				?.Log("Added application pool {IisAppPool}, useHighPrivilegedAccount: {useHighPrivilegedAccount}",
					addedPool.Name, sampleAppShouldUseHighPrivilegedAccount);

			ClearEnvVarsForSampleAppPool(serverManager);
			AddEnvVarsForSampleAppPool(serverManager, envVarsToSetForSampleAppPool);
		}

		private void AddSampleApp(ServerManager serverManager)
		{
			_logger.Debug()?.Log("Adding application {IisApp}...", Consts.SampleApp.RootUrlPath);
			var site = serverManager.Sites[Consts.SampleApp.SiteName];
			var existingApp = site.Applications[Consts.SampleApp.RootUrlPath];
			if (existingApp != null) site.Applications.Remove(existingApp);
			var addedApp = site.Applications.Add(Consts.SampleApp.RootUrlPath,
				Path.Combine(FindSolutionRoot().FullName, Consts.SampleApp.SrcDirPathRelativeToSolutionRoot));
			addedApp.ApplicationPoolName = Consts.SampleApp.AppPoolName;
			_logger.Debug()?.Log("Added application {IisApp}", Consts.SampleApp.RootUrlPath);
		}

		private ObjectState? TryGetAppPoolState(ApplicationPool appPool)
		{
			try
			{
				return appPool.State;
			}
			catch (COMException ex)
			{
				_logger.Debug()?.LogException(ex, "Failed to get IIS application pool `{IisAppPool}' state - returning null", appPool.Name);
				return null;
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

			var attemptNumber = 0;
			var alreadyInIngState = false;
			var timerSinceStart = Stopwatch.StartNew();
			while (true)
			{
				++attemptNumber;

				var currentState = TryGetAppPoolState(appPool);
				if (currentState != null)
				{
					if (currentState == targetState)
					{
						timerSinceStart.Stop();
						_logger.Info()
							?.Log("IIS application pool `{IisAppPoolName}' changed to target state {IisAppPoolState}." +
								" Time elapsed: {IisAppPoolStateChangeTimeSeconds}s." +
								" Attempt to verify #{AttemptNumber} out of {MaxNumberOfAttempts}.",
								appPool.Name, targetState,
								timerSinceStart.Elapsed.TotalSeconds,
								attemptNumber, StateChangeConsts.MaxNumberOfAttemptsToVerify);
						return;
					}

					if (!alreadyInIngState)
					{
						if (currentState == ingState)
						{
							alreadyInIngState = true;
							_logger.Debug()
								?.Log("IIS application pool `{IisAppPoolName}' is already in {IisAppPoolState} state - " +
									"so there is no need to run the change state action, " +
									"we just need to wait until application pool changes the state {IisAppPoolState}...",
									appPool.Name, currentState, targetState);
						}
						else
						{
							_logger.Debug()?.Log("{StateChanging} IIS application pool `{IisAppPoolName}'...", ingState, appPool.Name);
							changeAction(appPool);
						}
					}
				}

				if (attemptNumber == StateChangeConsts.MaxNumberOfAttemptsToVerify)
				{
					var ex = new InvalidOperationException($"Could not change IIS application pool `{appPool.Name}' state to {targetState}." +
						$" Time elapsed: {timerSinceStart.Elapsed.TotalSeconds}s." +
						$" Last seen state: {currentState}");
					_logger.Error()
						?.LogException(ex,
							"Reached max number of attempts to verify if application pool changed to target state - throwing an exception...");
					throw ex;
				}

				if (attemptNumber >= StateChangeConsts.LogMessageAfterNInitialAttempts
					&& attemptNumber % StateChangeConsts.LogMessageEveryNAttempts == 0)
				{
					_logger.Debug()
						?.Log("IIS application pool `{IisAppPoolName}' still did NOT change to target state {IisAppPoolState}." +
							" Last seen state: {IisAppPoolState}. " +
							" Time elapsed: {IisAppPoolStateChangeTimeSeconds}s." +
							" Attempt to verify #{AttemptNumber} out of {MaxNumberOfAttempts}. " +
							" Waiting {WaitBetweenVerifyAttemptsMs}ms before the next attempt..." +
							" This message is printed only every {LogMessageEveryNAttempts} attempts",
							appPool.Name, targetState,
							currentState,
							timerSinceStart.Elapsed.TotalSeconds,
							attemptNumber, StateChangeConsts.MaxNumberOfAttemptsToVerify,
							StateChangeConsts.WaitBetweenVerifyAttemptsMs,
							StateChangeConsts.LogMessageEveryNAttempts);
				}
				Thread.Sleep(StateChangeConsts.WaitBetweenVerifyAttemptsMs);
			}
		}

		private DirectoryInfo FindSolutionRoot()
		{
			var solutionFileName = "ElasticApmAgent.sln";

			var currentDirectory = Directory.GetCurrentDirectory();
			_logger.Debug()?.Log("Looking for solution root... Current directory: `{Directory}'", currentDirectory);
			var candidateDirectory = new DirectoryInfo(currentDirectory);
			do
			{
				if (File.Exists(Path.Combine(candidateDirectory.FullName, solutionFileName)))
				{
					_logger.Debug()?.Log("Found solution root: `{Directory}'", candidateDirectory);
					return candidateDirectory;
				}

				candidateDirectory = candidateDirectory.Parent;
			} while (candidateDirectory != null);

			throw new InvalidOperationException($"Could not find solution root directory from the current directory `{currentDirectory}'");
		}

		private ConfigurationElementCollection GetEnvVarsConfigCollectionForSampleAppPool(ServerManager serverManager)
		{
			var config = serverManager.GetApplicationHostConfiguration();
			var appPoolsSection = config.GetSection("system.applicationHost/applicationPools");
			var appPoolsCollection = appPoolsSection.GetCollection();
			var sampleAppPoolAddElement = FindConfigurationElement(appPoolsCollection, "add", "name", Consts.SampleApp.AppPoolName);
			if (sampleAppPoolAddElement == null)
				throw new InvalidOperationException($"Element <add> for application pool {Consts.SampleApp.AppPoolName} not found");

			return sampleAppPoolAddElement.GetCollection("environmentVariables");
		}

		private void AddEnvVarsForSampleAppPool(ServerManager serverManager, Dictionary<string, string> envVarsToSetForSampleAppPool)
		{
			var envVarsCollection = GetEnvVarsConfigCollectionForSampleAppPool(serverManager);
			foreach (var envVarNameValue in envVarsToSetForSampleAppPool)
			{
				var envVarAddElement = envVarsCollection.CreateElement("add");
				envVarAddElement["name"] = envVarNameValue.Key;
				envVarAddElement["value"] = envVarNameValue.Value;
				envVarsCollection.Add(envVarAddElement);
				_logger.Debug()
					?.Log("Added environment variable `{EnvVarName}'=`{EnvVarValue}' to application pool {IisAppPool}",
						envVarNameValue.Key, envVarNameValue.Value, Consts.SampleApp.AppPoolName);
			}
		}

		private void ClearEnvVarsForSampleAppPool(ServerManager serverManager) =>
			GetEnvVarsConfigCollectionForSampleAppPool(serverManager).Clear();

		private static ConfigurationElement FindConfigurationElement(
			ConfigurationElementCollection collection,
			string elementTagName,
			params string[] keyValues
		)
		{
			foreach (var element in collection)
			{
				if (string.Equals(element.ElementTagName, elementTagName, StringComparison.OrdinalIgnoreCase))
				{
					var matches = true;
					for (var i = 0; i < keyValues.Length; i += 2)
					{
						var o = element.GetAttributeValue(keyValues[i]);
						string value = null;
						if (o != null) value = o.ToString();
						if (!string.Equals(value, keyValues[i + 1], StringComparison.OrdinalIgnoreCase))
						{
							matches = false;
							break;
						}
					}
					if (matches) return element;
				}
			}
			return null;
		}

		internal void LogIisApplicationHostConfig()
		{
			// %WinDir%\System32\Inetsrv\Config\applicationHost.config
			const string winDirEnvVarName = "WinDir";
			var winDirEnvVarValue = Environment.GetEnvironmentVariable(winDirEnvVarName);
			if (winDirEnvVarValue == null)
			{
				_logger.Error()?.Log("{EnvVarName} is not set", winDirEnvVarName);
				return;
			}

			var filePath = winDirEnvVarValue + @"\System32\Inetsrv\Config\applicationHost.config";
			string[] lines;
			try
			{
				lines = File.ReadAllLines(filePath);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Exception thrown while trying to read IIS config file `{FilePath}'" +
					" ({EnvVarName} is `{EnvVarValue}')", filePath, winDirEnvVarName, winDirEnvVarValue);
				return;
			}

			var interestingLines = new List<string>(lines.Length);
			foreach (var line in lines) if (line.Contains("elastic", StringComparison.OrdinalIgnoreCase)) interestingLines.Add(line);
			_logger.Debug()?.Log("Found {NumberOfLines} interesting lines in IIS config file `{FilePath}'", interestingLines.Count, filePath);
			foreach (var line in interestingLines) _logger.Debug()?.Log("{Line}", TextUtils.Indent(line));
		}
	}
}
