using System;

namespace Elastic.Apm.Tests.TestHelpers
{
	/// <summary>
	/// Credit: https://github.com/xunit/samples.xunit/
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class TestPriorityAttribute : Attribute
	{
		public TestPriorityAttribute(int priority) => Priority = priority;

		public int Priority { get; private set; }
	}
}
