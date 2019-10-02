using System;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal class DummyTestException: Exception
	{
		internal const string DefaultMessage = "Dummy exception message";
		internal DummyTestException() : this(DefaultMessage) { }
		internal DummyTestException(string message) : base(message) { }
		internal DummyTestException(string message, Exception cause) : base(message, cause) { }
	}
}
