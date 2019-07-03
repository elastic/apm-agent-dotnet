using System;

namespace Elastic.Apm.Tests.TestHelpers
{
	public static class FluentAssertionsUtils
	{
		public static Action AsAction(Action action) => action;
	}
}
