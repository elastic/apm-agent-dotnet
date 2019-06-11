using System;
using Xunit;
using Elastic.Apm.Tests.MockApmServer;
using Microsoft.Web.Administration;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class TestsBase
	{
		internal readonly MockApmServerSingleton _mockApmServerSingleton = new MockApmServerSingleton();

		protected TestsBase()
		{
			_mockApmServerSingleton.EnsureServerIsRunning();

			IisAdministration.AddSampleAppToIis();
		}
	}
}
