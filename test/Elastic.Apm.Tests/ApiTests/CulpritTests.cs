using System;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using LibraryNamespace;
using Test.Application;
using Xunit;

namespace Elastic.Apm.Tests.ApiTests
{
	public class CulpritTests
	{
		[Fact]
		public void GetCulpritTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					throw new Exception("TestMst");
				}
				catch (Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().Be("Elastic.Apm.Tests.ApiTests.CulpritTests");
		}

		[Fact]
		public void ShouldNotReturnNotIncludedNamespaces()
		{
			var payloadSender = new MockPayloadSender();
			var config = new MockConfigSnapshot(applicationNamespaces: "System.");
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: config)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					throw new Exception("TestMst");
				}
				catch (Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().NotBe("Elastic.Apm.Tests.BasicAgentTests");
		}

		[Fact]
		public void ShouldReturnIncludedNamespaces()
		{
			var payloadSender = new MockPayloadSender();
			var config = new MockConfigSnapshot(applicationNamespaces: "MyApp1, Elastic.Apm.Tests., MyApp2");
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: config)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					throw new Exception("TestMst");
				}
				catch (Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().Be("Elastic.Apm.Tests.ApiTests.CulpritTests");
		}

		/// <summary>
		/// Makes sure that a library namespace is not captured as the culprit
		/// </summary>
		[Fact]
		public void GetCulpritWithLibraryFrames()
		{
			var payloadSender = new MockPayloadSender();
			var config = new MockConfigSnapshot(excludedNamespaces: "LibraryNamespace");
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: config)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					new ApplicationClass().Method1();
				}
				catch (Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().Be("Test.Application.ApplicationClass");
		}

		/// <summary>
		/// Same as <see cref="GetCulpritWithLibraryFrames"/> but with multiple namespaces
		/// </summary>
		[Fact]
		public void GetCulpritWithLibraryFramesWithMultipleNamespaces()
		{
			var payloadSender = new MockPayloadSender();
			var config = new MockConfigSnapshot(excludedNamespaces: "MyLib1, LibraryNamespace, MyLib2");
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: config)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					new ApplicationClass().Method1();
				}
				catch (Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().Be("Test.Application.ApplicationClass");
		}
	}
}

// Dummy application namespace
namespace Test.Application
{
	public class ApplicationClass
	{
		public void Method1() => new LibraryClass().LibraryMethod();
	}
}

// Dummy library namespace
namespace LibraryNamespace
{
	public class LibraryClass
	{
		public void LibraryMethod() => throw new Exception("This is a test expcetion");
	}
}
