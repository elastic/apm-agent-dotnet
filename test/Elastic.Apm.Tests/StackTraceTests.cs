using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Extensions;
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
		/// Captures a Span
		/// and makes sure that we have at least 1 stack frame with LineNo != 0
		/// This test assumes that LineNo capturing is enabled and pdb files are also present
		/// </summary>
		[Fact]
		public void StackTraceContainsLineNumber()
			=> AssertWithAgent("-1", "-1", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().NotBeEmpty();
				var stackFrames = payloadSender.FirstSpan?.StackTrace;
				stackFrames.Should().NotBeEmpty().And.Contain(frame => frame.LineNo != 0);
			});

		/// <summary>
		/// Makes sure that the name of the async method is captured correctly
		/// Also asserts that the line number is not 0 in this case.
		/// See: https://github.com/elastic/apm-agent-dotnet/pull/253#discussion_r291835766
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
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace.Should()
				.Contain(m => m.Function == nameof(ClassWithAsync.TestMethodAsync) && m.LineNo != 0);
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
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace.Should().Contain(m => m.Function == nameof(ClassWithSyncMethods.MoveNext));
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace.Should().Contain(m => m.Function == nameof(ClassWithSyncMethods.M2));
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

			(payloadSender.Errors.First() as Error)?.Exception.StackTrace.Should()
				.Contain(m => m.FileName == typeof(Base).FullName
					&& m.Function == nameof(Base.Method1)
					&& m.Module == typeof(Base).Assembly.FullName
				);

			(payloadSender.Errors.First() as Error)?.Exception.StackTrace.Should()
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
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace.Should().NotContain(frame => string.IsNullOrWhiteSpace(frame.FileName));
		}

		[Fact]
		public void InheritedChainWithVirtualMethod()
		{
			Base testClass = new Derived();

			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
				Assert.Throws<Exception>(() => { agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => { testClass.MyMethod(); }); });

			payloadSender.Errors.First().Should().NotBeNull();
			payloadSender.Errors.First().Should().BeOfType(typeof(Error));

			//note: since filename is used on the UI (and there is no other way to show the classname, we misuse this field
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace[0].FileName.Should().Be(typeof(Derived).FullName);
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace[0].Function.Should().Be(nameof(Derived.MethodThrowingIDerived));

			(payloadSender.Errors.First() as Error)?.Exception.StackTrace[1].FileName.Should().Be(typeof(Derived).FullName);
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace[1].Function.Should().Be(nameof(Base.MyMethod));
		}

		[Fact]
		public void InheritedChain()
		{
			Base testClass = new Derived();

			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
				Assert.Throws<Exception>(() => { agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => { testClass.JustThrow(); }); });

			payloadSender.Errors.First().Should().NotBeNull();
			payloadSender.Errors.First().Should().BeOfType(typeof(Error));

			//note: since filename is used on the UI (and there is no other way to show the classname, we misuse this field
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace[0].FileName.Should().Be(typeof(Base).FullName);
			(payloadSender.Errors.First() as Error)?.Exception.StackTrace[0].Function.Should().Be(nameof(Base.JustThrow));
		}

		[Fact]
		public void StackTraceLimit0SpanFramesMinDurationNegative() =>
			AssertWithAgent("0", "-1", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().BeNull();
			});

		[Fact]
		public void StackTraceLimitNegativeSpanFramesMinDurationNegative() =>
			AssertWithAgent("-1", "-1", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().NotBeEmpty();
				// contains all frame which depends on the test runner
				// therefore the exact StackTrace.Count depends on where you run the test
				payloadSender.FirstSpan.StackTrace.Should().HaveCountGreaterThan(10);
			});

		[Fact]
		public void StackTraceLimit2SpanFramesMinDurationNegative() =>
			AssertWithAgent("2", "-1", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().NotBeEmpty();
				payloadSender.FirstSpan.StackTrace.Count.Should().Be(2);

				//We don't filter out frames from the agent, therefore the current implementation will contain 2 agent frames on top
				payloadSender.FirstSpan.StackTrace[0].Function.Should().Be("End");
				payloadSender.FirstSpan.StackTrace[1].Function.Should().Be("CaptureSpan");
			});

		[Fact]
		public void StackTraceLimit0SpanFramesMinDuration0() =>
			AssertWithAgent("0", "0", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().BeNull();
			});

		[Fact]
		public void StackTraceLimitNegativeSpanFramesMinDuration0() =>
			AssertWithAgent("-1", "0", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().BeNull();
			});

		[Fact]
		public void StackTraceLimit2SpanFramesMinDuration0() =>
			AssertWithAgent("2", "0", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().BeNull();
			});

		[Fact]
		public void StackTraceLimit0SpanFramesMinDuration100() =>
			AssertWithAgent("0", "100", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().BeNullOrEmpty();
			}, 150);

		[Fact]
		public void StackTraceLimitNegativeSpanFramesMinDuration100() =>
			AssertWithAgent("-1", "100", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().NotBeEmpty();
				payloadSender.FirstSpan.StackTrace.Should().HaveCountGreaterThan(10);

				//We don't filter out frames from the agent, therefore the current implementation will contain 2 agent frames on top
				payloadSender.FirstSpan.StackTrace[0].Function.Should().Be("End");
				payloadSender.FirstSpan.StackTrace[1].Function.Should().Be("CaptureSpan");
			}, 150);

		[Fact]
		public void StackTraceLimit2SpanFramesMinDuration100() =>
			AssertWithAgent("2", "100", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().NotBeEmpty();
				payloadSender.FirstSpan.StackTrace.Should().HaveCount(2);

				//We don't filter out frames from the agent, therefore the current implementation will contain 2 agent frames on top
				payloadSender.FirstSpan.StackTrace[0].Function.Should().Be("End");
				payloadSender.FirstSpan.StackTrace[1].Function.Should().Be("CaptureSpan");
			}, 150);

		[Fact]
		public void DefaultStackTraceLimitAndSpanFramesMinDuration_ShortSpan()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", t
					=>
				{
					t.CaptureSpan("span", "span", () => { });
				});
			}

			payloadSender.FirstSpan.Should().NotBeNull();

			if (payloadSender.FirstSpan.Duration < ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds)
				payloadSender.FirstSpan.StackTrace.Should().BeNullOrEmpty();
			else
				payloadSender.FirstSpan.StackTrace.Should().NotBeNullOrEmpty();
		}

		/// <summary>
		/// Makes sure the stacktrace is not captured when the span is shorter than spanFramesMinDuration.
		/// In https://github.com/elastic/apm-agent-dotnet/pull/451 we found that with the
		/// default spanFramesMinDuration (currently 5)
		/// the test became in CI flaky - sometimes the spans in CI are shorter than 5 ms, sometimes not.
		/// To avoid this in this test we set spanFramesMinDuration to 10000 and execute a span with empty method body.
		/// </summary>
		[Fact]
		public void StackTraceLimit2SpanFramesMinDuration1000NoSleepInSpan() =>
			AssertWithAgent("2", "1000", payloadSender =>
			{
				payloadSender.FirstSpan.Should().NotBeNull();
				payloadSender.FirstSpan.StackTrace.Should().BeNullOrEmpty();
			});

		[Fact]
		public void DefaultStackTraceLimitAndSpanFramesMinDuration_LongSpan()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", t
					=>
				{
					t.CaptureSpan("span", "span", () =>
					{
						WaitHelpers.SleepMinimum();
						Thread.Sleep((int)ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds);
					});
				});
			}

			payloadSender.FirstSpan.Should().NotBeNull();
			payloadSender.FirstSpan.StackTrace.Should().NotBeNullOrEmpty();
		}

		[Fact]
		public void ErrorWithDefaultStackTraceLimit()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() =>
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "Test", t
						=>
					{
						t.CaptureSpan("span", "span", () => { RecursiveCall100XAndThrow(0); });
					});
				});
			}

			payloadSender.FirstError.Should().NotBeNull();
			payloadSender.FirstError.Exception.StackTrace.Should().NotBeNullOrEmpty();
			payloadSender.FirstError.Exception.StackTrace.Should().HaveCount(ConfigConsts.DefaultValues.StackTraceLimit);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		private static void RecursiveCall100XAndThrow(int i)
		{
			if (i == 100)
				throw new Exception("TestException");

			// ReSharper disable once TailRecursiveCall - this method creates a stack with 100 frames on purpose.
			RecursiveCall100XAndThrow(i + 1);
		}

		[Fact]
		public void ErrorWithDefaultSpanFramesMinDurationInMillisecondsAndStackTraceLimit2()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(configurationReader: new TestAgentConfigurationReader(stackTraceLimit: "2"),
				payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() =>
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "Test", t
						=>
					{
						t.CaptureSpan("span", "span", () => { throw new Exception("TestException"); });
					});
				});
			}

			payloadSender.FirstError.Should().NotBeNull();
			payloadSender.FirstError.Exception.StackTrace.Should().NotBeNullOrEmpty();
			payloadSender.FirstError.Exception.StackTrace.Should().HaveCount(2);
		}

		/// <summary>
		/// Makes sure that even if SpanFramesMinDuration is 0 (meaning no stacktrace is captured for spans)
		/// the agent still captures stacktraces for error.
		/// </summary>
		/// <exception cref="Exception"></exception>
		[Fact]
		public void ErrorWithStackTraceLimit2WithSpanFramesMinDurationNegative()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(
				configurationReader: new TestAgentConfigurationReader(stackTraceLimit: "2", spanFramesMinDurationInMilliseconds: "-2"),
				payloadSender: payloadSender)))
			{
				Assert.Throws<Exception>(() =>
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "Test", t
						=>
					{
						t.CaptureSpan("span", "span", () => { throw new Exception("TestException"); });
					});
				});
			}

			payloadSender.FirstError.Should().NotBeNull();
			payloadSender.FirstError.Exception.StackTrace.Should().NotBeNullOrEmpty();
			payloadSender.FirstError.Exception.StackTrace.Should().HaveCount(2);
		}

		private static void AssertWithAgent(string stackTraceLimit, string spanFramesMinDuration, Action<MockPayloadSender> assertAction,
			int sleepLength = 0
		)
		{
			var payloadSender = new MockPayloadSender();

			using (var agent =
				new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					configurationReader: new TestAgentConfigurationReader(new NoopLogger(), stackTraceLimit: stackTraceLimit,
						spanFramesMinDurationInMilliseconds: spanFramesMinDuration))))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", t
					=>
				{
					t.CaptureSpan("span", "span", () => { Thread.Sleep(sleepLength); });
				});
			}

			assertAction(payloadSender);
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
