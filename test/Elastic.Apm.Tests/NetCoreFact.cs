// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.InteropServices;
using Elastic.Apm.Helpers;
using Xunit;

namespace Elastic.Apm.Tests
{
	public sealed class NetCoreFact : FactAttribute
	{
		public NetCoreFact()
		{
			if (!RuntimeInformation.FrameworkDescription.StartsWith(PlatformDetection.DotNetCoreDescriptionPrefix))
			{
				Skip =
					$"{nameof(NetCoreFact)} tests only run on .NET Core - test was executed on {RuntimeInformation.FrameworkDescription} - therefore test will be skipped";
			}
		}
	}
}
