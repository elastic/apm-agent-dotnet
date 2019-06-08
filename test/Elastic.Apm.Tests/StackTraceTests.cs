using System;
using System.Diagnostics;
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
		/// Also asserts that the line number is 0 in this case - that is because the fixed method name
		/// does not match the line number in the state machine, so the agent sends LineNo 0.
		/// </summary>
		[Fact]
		public async Task AsyncCallStackTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				await Assert.ThrowsAsync<Exception>(async () =>
				{
					await agent.Tracer.CaptureTransaction("TestTransaction", "Test", async () =>
					{
						var classWithAsync = new ClassWithAsync();
						await classWithAsync.TestMethodAsync();
					});
				});
			}

			payloadSender.Errors.Should().NotBeEmpty();
			(payloadSender.Errors.First() as Error).Should().NotBeNull();
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should()
				.Contain(m => m.Function == nameof(ClassWithAsync.TestMethodAsync) && m.LineNo == 0);
		}

		/// <summary>
		/// Makes sure that if a non-async method is named 'MoveNext', it does not cause any trouble
		/// </summary>
		[Fact]
		public void CallStackWithMoveNextWithoutAsync()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() =>
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "Test", () =>
					{
						var classWithSyncMethods = new ClassWithSyncMethods();
						classWithSyncMethods.MoveNext();
					});
				});
			}

			payloadSender.Errors.Should().NotBeEmpty();
			(payloadSender.Errors.First() as Error).Should().NotBeNull();
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should().Contain(m => m.Function == nameof(ClassWithSyncMethods.MoveNext));
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should().Contain(m => m.Function == nameof(ClassWithSyncMethods.M2));
		}

		/// <summary>
		/// Makes sure that the typename and the method name are captured correctly
		/// </summary>
		[Fact]
		public void TypeAndMethodNameTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() =>
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "Test", () =>
					{
						Base testClass = new Derived();
						testClass.Method1();
					});
				});
			}

			payloadSender.Errors.Should().NotBeEmpty();
			(payloadSender.Errors.First() as Error).Should().NotBeNull();

			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should()
				.Contain(m => m.FileName == typeof(Base).FullName
					&& m.Function == nameof(Base.Method1)
					&& m.Module == typeof(Base).Assembly.FullName
				);

			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should()
				.Contain(m => m.FileName == typeof(Derived).FullName
					&& m.Function == nameof(Derived.TestMethod)
					&& m.Module == typeof(Derived).Assembly.FullName);
		}

		/// <summary>
		/// Makes sure that the filename is never null or empty in the call stack, since it's a required field.
		/// </summary>
		[Fact]
		public void StackTraceWithLambda()
		{
			Action action = () => { TestMethod(); };

			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			Assert.Throws<Exception>(() => { agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => { action(); }); });

			payloadSender.Errors.Should().NotBeEmpty();
			(payloadSender.Errors.First() as Error).Should().NotBeNull();
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace.Should().NotContain(frame => string.IsNullOrWhiteSpace(frame.FileName));
		}

		[Fact]
		public void InheritedChainWithVirtualMethod()
		{
			Base testClass = new Derived();

			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() => { agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => { testClass.MyMethod(); }); });
			}

			payloadSender.Errors.First().Should().NotBeNull();
			payloadSender.Errors.First().Should().BeOfType(typeof(Error));

			//note: since filename is used on the UI (and there is no other way to show the classname, we misuse this field
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace[0].FileName.Should().Be(typeof(Derived).FullName);
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace[0].Function.Should().Be(nameof(Derived.MethodThrowingIDerived));

			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace[1].FileName.Should().Be(typeof(Derived).FullName);
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace[1].Function.Should().Be(nameof(Base.MyMethod));
		}

		[Fact]
		public void InheritedChain()
		{
			Base testClass = new Derived();

			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() => { agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => { testClass.JustThrow(); }); });
			}

			payloadSender.Errors.First().Should().NotBeNull();
			payloadSender.Errors.First().Should().BeOfType(typeof(Error));

			//note: since filename is used on the UI (and there is no other way to show the classname, we misuse this field
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace[0].FileName.Should().Be(typeof(Base).FullName);
			(payloadSender.Errors.First() as Error)?.Exception.Stacktrace[0].Function.Should().Be(nameof(Base.JustThrow));
		}

		private void TestMethod() => InnerTestMethod(() => throw new Exception("TestException"));

		private void InnerTestMethod(Action actionToRun)
		{
			try
			{
				actionToRun();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
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

	internal class Base
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void JustThrow() => throw new Exception("Test exception in Base.JustThrow");

		[MethodImpl(MethodImplOptions.NoInlining)]
		public virtual void MyMethod()
			=> MethodThrowingInBase();

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void MethodThrowingInBase() => throw new Exception("Test exception in Base");

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal void Method1() => TestMethod();

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal virtual void TestMethod()
			=> Debug.WriteLine("test");
	}

	internal class Derived : Base
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override void MyMethod() => MethodThrowingIDerived();

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void MethodThrowingIDerived() => throw new Exception("Test exception in Derived");

		internal override void TestMethod() => throw new Exception("TestException");
	}
}
