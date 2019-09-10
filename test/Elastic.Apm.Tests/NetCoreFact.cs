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
