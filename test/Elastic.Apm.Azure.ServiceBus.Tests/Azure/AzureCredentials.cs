// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;
using ProcNet;

namespace Elastic.Apm.Azure.ServiceBus.Tests.Azure
{
	public class Unauthenticated : AzureCredentials
	{
	}

	public class AzureUserAccount : AzureCredentials
	{
	}

	public class ServicePrincipal : AzureCredentials
	{
		[JsonConstructor]
		private ServicePrincipal() { }

		[JsonProperty]
		public string ClientId { get; private set; }

		[JsonProperty]
		public string ClientSecret { get; private set; }

		[JsonProperty]
		public string TenantId { get; private set; }

		[JsonProperty]
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
			var runningInCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_ID"));
			if (runningInCi)
			{
				var clientId = Environment.GetEnvironmentVariable(ARM_CLIENT_ID);
				var clientSecret = Environment.GetEnvironmentVariable(ARM_CLIENT_SECRET);
				var tenantId = Environment.GetEnvironmentVariable(ARM_TENANT_ID);
				var subscriptionId = Environment.GetEnvironmentVariable(ARM_SUBSCRIPTION_ID);

				if (string.IsNullOrEmpty(clientId) ||
					string.IsNullOrEmpty(clientSecret) ||
					string.IsNullOrEmpty(tenantId) ||
					string.IsNullOrEmpty(subscriptionId))
					return new Unauthenticated();

				return new ServicePrincipal(clientId, clientSecret, tenantId, subscriptionId);
			}

			return LoggedIntoAccountWithAzureCli() ? new AzureUserAccount() : new Unauthenticated();
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
				var binary = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? "cmd"
					: "az";
				var args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
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
		/// A set of Azure credentials obtained from environment variables or a .credentials.json configuration file
		/// </summary>
		public static AzureCredentials Instance => _lazyCredentials.Value;

		public virtual void AddToArguments(StartArguments startArguments) { }
	}
}
