// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.InteropServices;
using Elastic.Apm.Helpers;
using Xunit;

namespace Elastic.Apm.Tests
{
	public sealed class NetCoreAndNetFact : FactAttribute
	{
		public NetCoreAndNetFact()
		{
			if (!(RuntimeInformation.FrameworkDescription.StartsWith(PlatformDetection.DotNetCoreDescriptionPrefix)
				|| (RuntimeInformation.FrameworkDescription.StartsWith(PlatformDetection.DotNetPrefix) &&
					!RuntimeInformation.FrameworkDescription.StartsWith(PlatformDetection.DotNetFullFrameworkDescriptionPrefix))))
			{
				Skip =
					$"{nameof(NetCoreAndNetFact)} tests only run on .NET Core and .NET 5 - test was executed on {RuntimeInformation.FrameworkDescription} - therefore test will be skipped";
			}
		}
	}
}
