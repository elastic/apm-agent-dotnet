using System;

namespace Elastic.Apm.Helpers
{
	internal class AssertionFailedException : Exception
	{
		internal AssertionFailedException(string message) : base(message) { }
	}
}
