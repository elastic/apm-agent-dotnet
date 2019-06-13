using System;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class AspNetFullFrameworkFact : FactAttribute
	{
		public AspNetFullFrameworkFact() => Skip = TestsEnabledDetector.ReasonWhyTestsAreSkipped;
	}
}
