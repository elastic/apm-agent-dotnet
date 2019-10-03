using Xunit;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.TestHelpers
{
	[XunitTestCaseDiscoverer(/* typeName: */ DynamicallySelectableTestCaseDiscoverer.ThisClassFullName
		, /* assemblyName (without file extension): */ DynamicallySelectableTestCaseDiscoverer.ThisClassAssemblyName)]
	public abstract class DynamicallySelectableFactAttribute : FactAttribute
	{
		/// <summary>
		/// Marks the test so that it will not be run
		/// </summary>
		/// <returns><c>null</c> if this test case is selected, otherwise a string containing the </returns>
		public abstract string ReasonNotSelected { get; }

		public override string Skip => DynamicallySelectableTestCaseDiscoverer.NotSelectedIsSkipped ? ReasonNotSelected : null;
	}
}
