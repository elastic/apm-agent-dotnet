// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using ProcNet;

namespace Elastic.Apm.Tests.Utilities.Azure
{
	/// <summary>
	/// Unauthenticated Azure credentials
	/// </summary>
	public class Unauthenticated : AzureCredentials;

	/// <summary>
	/// Azure credentials authentication with a User account.
	/// </summary>
	public class AzureUserAccount : AzureCredentials;

	public abstract class AzureCredentials
	{
		private static readonly Lazy<AzureCredentials> LazyCredentials =
			new(LoadCredentials, LazyThreadSafetyMode.ExecutionAndPublication);

		private static AzureCredentials LoadCredentials() =>
			LoggedIntoAccountWithAzureCli()
				? new AzureUserAccount()
				: new Unauthenticated();

		/// <summary>
		/// Checks that Azure CLI is installed and in the PATH, and is logged into an account
		/// </summary>
		/// <returns>true if logged in</returns>
		private static bool LoggedIntoAccountWithAzureCli()
		{
			try
			{
				// run azure CLI using cmd on Windows so that %~dp0 in az.cmd expands to
				// the path containing the cmd file.
				var binary = TestEnvironment.IsWindows
					? "cmd"
					: "az";
				var args = TestEnvironment.IsWindows
					? new[] { "/c", "az", "account", "show" }
					: new[] { "account", "show" };

				var result = Proc.Start(new StartArguments(binary, args));
				return result.Completed && result.ExitCode == 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}

		/// <summary>
		/// A set of Azure credentials obtained from environment variables or account authenticated with Azure CLI 2.0.
		/// If no credentials are found, an unauthenticated credential is returned.
		/// </summary>
		public static AzureCredentials Instance => LazyCredentials.Value;

		public virtual void AddToArguments(StartArguments startArguments) { }
	}
}
