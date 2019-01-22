using System.Reflection;
using System.Threading;
using Elastic.Apm.Tests.Mocks;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Very basic agent related tests
	/// </summary>
	public class BasicAgentTests
	{
		/// <summary>
		/// Creates a simple transaction.
		/// Makes sure that the agent reports the transaction with the correct agent version,
		/// which is the version of the Elastic.Apm assembly.
		/// </summary>
		[Fact]
		public void AgentVersion()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestName", "TestType", () => { Thread.Sleep(5); });
			}

			Assert.Equal(Assembly.Load("Elastic.Apm").GetName().Version.ToString(), payloadSender.Payloads[0].Service.Agent.Version);
		}
	}
}
