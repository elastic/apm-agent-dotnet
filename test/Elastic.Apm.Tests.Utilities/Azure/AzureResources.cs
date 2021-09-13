// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Tests.Utilities.Azure
{
	public static class AzureResources
	{
		public const int ResourceGroupMaxLength = 90;

		public static string CreateResourceGroupName(string suffix)
		{
			var prefix = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP_PREFIX");
			if (string.IsNullOrEmpty(prefix))
				prefix = "dotnet";

			// check that we'll be under the max length before calculating machine name truncation
			var maxMachineNameLength = ResourceGroupMaxLength - (prefix + "--" + suffix).Length;
			if (maxMachineNameLength < 0)
			{
				throw new AzureResourceException(
					$"resource group name {prefix}-${{machine-name}}-{suffix} is longer than the maximum resource group name length of {ResourceGroupMaxLength}");
			}

			var machineName = Environment.MachineName.ToLowerInvariant();
			if (machineName.Length > maxMachineNameLength)
				machineName = machineName.Substring(0, maxMachineNameLength);

			return $"{prefix}-{machineName}-{suffix}";
		}
	}
}
