using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Contains tests related to stack traces
	/// </summary>
	public class StackTraceTests
	{
		/// <summary>
		/// Captures an HTTP request
		/// and makes sure that we have at least 1 stack frame with LineNo != 0
		/// This test assumes that LineNo capturing is enabled.
		/// </summary>
		[Fact]
		public async Task HttpClientStackTrace()
		{
			var (listener, payloadSender, _) = HttpDiagnosticListenerTest.RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(uri: "http://localhost:8083/"))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			var stackFrames = payloadSender.FirstSpan?.StackTrace;

			stackFrames.Should().NotBeEmpty().And.Contain(frame => frame.LineNo != 0);
		}

		/// <summary>
		/// Makes sure that the name of the async method is captured correctly
		/// </summary>
		[Fact]
		public async Task AsyncCallStackTest()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await Assert.ThrowsAsync<Exception>(async () =>
			{
				await agent.Tracer.CaptureTransaction("TestTransaction", "Test", async () =>
				{
					var classWithAsync = new ClassWithAsync();
					await classWithAsync.TestMethodAsync();
				});
			});

			payloadSender.Errors.Should().NotBeEmpty();
			(payloadSender.Errors.First() as Error).Should().NotBeNull();
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should().Contain(m => m.Function == nameof(ClassWithAsync.TestMethodAsync));
		}


		/// <summary>
		/// Makes sure that if a non-async method is named 'MoveNext', it does not cause any trouble
		/// </summary>
		[Fact]
		public void CallStackWithMoveNextWithoutAsync()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			Assert.Throws<Exception>(() =>
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", () =>
				{
					var classWithSyncMethods = new ClassWithSyncMethods();
					classWithSyncMethods.MoveNext();
				});
			});

			payloadSender.Errors.Should().NotBeEmpty();
			(payloadSender.Errors.First() as Error).Should().NotBeNull();
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should().Contain(m => m.Function == nameof(ClassWithSyncMethods.MoveNext));
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should().Contain(m => m.Function == nameof(ClassWithSyncMethods.M2));
		}

		private class ClassWithSyncMethods
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			internal void MoveNext() => M2();

			[MethodImpl(MethodImplOptions.NoInlining)]
			internal void M2() => throw new Exception("bamm");
		}

		private class ClassWithAsync
		{
			internal async Task TestMethodAsync()
			{
				await Task.Delay(5);
				throw new Exception("bamm");
			}
		}
	}
}
