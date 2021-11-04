// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using ProcNet;

namespace Elastic.Apm.Tests.Utilities.Azure
{
	/// <summary>
	/// Unauthenticated Azure credentials
	/// </summary>
	public class Unauthenticated : AzureCredentials
	{
	}

	/// <summary>
	/// Azure credentials authentication with a User account.
	/// </summary>
	public class AzureUserAccount : AzureCredentials
	{
	}

	/// <summary>
	/// Azure credentials authenticated with a Service Principal
	/// </summary>
	public class ServicePrincipal : AzureCredentials
	{
		[JsonConstructor]
		private ServicePrincipal() { }

		[JsonProperty("clientId")]
		public string ClientId { get; private set; }

		[JsonProperty("clientSecret")]
		public string ClientSecret { get; private set; }

		[JsonProperty("tenantId")]
		public string TenantId { get; private set; }

		[JsonProperty("subscriptionId")]
		public string SubscriptionId { get; private set; }

		public ServicePrincipal(string clientId, string clientSecret, string tenantId, string subscriptionId)
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
			TenantId = tenantId;
			SubscriptionId = subscriptionId;
		}
		public override void AddToArguments(StartArguments startArguments)
		{
			startArguments.Environment ??= new Dictionary<string, string>();
			startArguments.Environment[ARM_CLIENT_ID] = ClientId;
			startArguments.Environment[ARM_CLIENT_SECRET] = ClientSecret;
			startArguments.Environment[ARM_SUBSCRIPTION_ID] = SubscriptionId;
			startArguments.Environment[ARM_TENANT_ID] = TenantId;
		}
	}

	public abstract class AzureCredentials
	{
		// ReSharper disable InconsistentNaming
		protected const string ARM_CLIENT_ID = nameof(ARM_CLIENT_ID);
		protected const string ARM_CLIENT_SECRET = nameof(ARM_CLIENT_SECRET);
		protected const string ARM_TENANT_ID = nameof(ARM_TENANT_ID);
		protected const string ARM_SUBSCRIPTION_ID = nameof(ARM_SUBSCRIPTION_ID);
		// ReSharper restore InconsistentNaming

		private static readonly Lazy<AzureCredentials> _lazyCredentials =
			new Lazy<AzureCredentials>(LoadCredentials, LazyThreadSafetyMode.ExecutionAndPublication);

		private static AzureCredentials LoadCredentials()
		{
			if (TestEnvironment.IsCi)
			{
				var credentialsFile = Path.Combine(SolutionPaths.Root, ".credentials.json");
				if (!File.Exists(credentialsFile))
					return new Unauthenticated();

				try
				{
					using var fileStream = new FileStream(credentialsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
					using var streamReader = new StreamReader(fileStream);
					using var jsonTextReader = new JsonTextReader(streamReader);
					var serializer = new JsonSerializer();
					return serializer.Deserialize<ServicePrincipal>(jsonTextReader);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					return new Unauthenticated();
				}
			}

			return LoggedIntoAccountWithAzureCli()
				? new AzureUserAccount()
				: new Unauthenticated();
		}

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
		public static AzureCredentials Instance => _lazyCredentials.Value;

		public virtual void AddToArguments(StartArguments startArguments) { }
	}
}
