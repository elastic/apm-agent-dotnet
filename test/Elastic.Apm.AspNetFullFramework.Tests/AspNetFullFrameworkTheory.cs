using System;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class AspNetFullFrameworkTheory : TheoryAttribute
	{
		public AspNetFullFrameworkTheory() => Skip = TestsEnabledDetector.ReasonWhyTestsAreSkipped;
	}
}
