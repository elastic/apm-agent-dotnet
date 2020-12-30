// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.HelpersTests;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.ApiTests
{
	/// <summary>
	/// Tests the API for manual instrumentation.
	/// Only tests scenarios when using the convenient API and only test spans.
	/// Transactions are covered by <see cref="ConvenientApiTransactionTests" />.
	/// Scenarios with manually calling <see cref="Tracer.StartTransaction" />,
	/// <see cref="Transaction.StartSpan" />, <see cref="Span.StartSpan" />, <see cref="Transaction.End" />
	/// are covered by <see cref="ApiTests" />
	/// Very similar to <see cref="ConvenientApiTransactionTests" />. The test cases are the same,
	/// but this one tests the CaptureSpan method - including every single overload.
	/// Tests with postfix `_OnSubSpan` do exactly the same as tests without the postfix, the only difference is
	/// that those create a span on another span (aka sub span) and test things on a sub span. Tests without the
	/// `_OnSubSpan` postfix typically start the span on a transaction and not on a span.
	/// </summary>
	public class ConvenientApiSpanTests
	{
		private const string ExceptionMessage = "Foo";
		private const string SpanName = "TestSpan";

		private const string SpanType = "TestSpan";
		private const string TransactionName = "ConvenientApiTest";

		private const string TransactionType = "Test";

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Action,string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) into the CaptureSpan method
		/// and it makes sure that the span is captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleAction()
			=> AssertWith1TransactionAnd1Span(t => { t.CaptureSpan(SpanName, SpanType, () => { WaitHelpers.Sleep2XMinimum(); }); });

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Action,string,string)" /> method with an exception.
		/// It wraps a fake span (Thread.Sleep) that throws an exception into the CaptureSpan method
		/// and it makes sure that the span and the exception are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithException()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				Action act = () =>
				{
					t.CaptureSpan(SpanName, SpanType, new Action(() =>
					{
						WaitHelpers.Sleep2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					}));
				};
				act.Should().Throw<InvalidOperationException>();
			});

		[Fact]
		public void SimpleActionWithException_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanAnd1ErrorOnSubSpan(s =>
			{
				Action act = () =>
				{
					s.CaptureSpan(SpanName, SpanType, new Action(() =>
					{
						WaitHelpers.Sleep2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					}));
				};
				act.Should().Throw<InvalidOperationException>();
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Action,string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) into the CaptureSpan method with an <see cref="Action{T}" /> parameter
		/// and it makes sure that the span is captured by the agent and the <see cref="Action{ISpan}" /> parameter is not null
		/// </summary>
		[Fact]
		public void SimpleActionWithParameter()
			=> AssertWith1TransactionAnd1Span(t =>
			{
				t.CaptureSpan(SpanName, SpanType,
					s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();
					});
			});

		[Fact]
		public void SimpleActionWithParameter_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanOnSubSpan(t =>
			{
				t.CaptureSpan(SpanName, SpanType,
					s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();
					});
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Action,string,string)" /> method with an
		/// exception.
		/// It wraps a fake span (Thread.Sleep) that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the <see cref="ISpan" /> parameter is not
		/// null
		/// </summary>
		[Fact]
		public void SimpleActionWithExceptionAndParameter()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				Action act = () =>
				{
					t.CaptureSpan(SpanName, SpanType, new Action<ISpan>(s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					}));
				};
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		[Fact]
		public void SimpleActionWithExceptionAndParameter_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanAnd1ErrorOnSubSpan(s =>
			{
				Action act = () =>
				{
					s.CaptureSpan(SpanName, SpanType, new Action<ISpan>(subSpan =>
					{
						subSpan.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					}));
				};
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) with a return value into the CaptureSpan method
		/// and it makes sure that the span is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnType()
			=> AssertWith1TransactionAnd1Span(t =>
			{
				var res = t.CaptureSpan(SpanName, SpanType, () =>
				{
					WaitHelpers.Sleep2XMinimum();
					return 42;
				});

				res.Should().Be(42);
			});

		[Fact]
		public void SimpleActionWithReturnType_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanOnSubSpan(s =>
			{
				var res = s.CaptureSpan(SpanName, SpanType, () =>
				{
					WaitHelpers.Sleep2XMinimum();
					return 42;
				});

				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) with a return value into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span is captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndParameter()
			=> AssertWith1TransactionAnd1Span(t =>
			{
				var res = t.CaptureSpan(SpanName, SpanType, s =>
				{
					t.Should().NotBeNull();
					WaitHelpers.Sleep2XMinimum();
					return 42;
				});

				res.Should().Be(42);
			});

		[Fact]
		public void SimpleActionWithReturnTypeAndParameter_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanOnSubSpan(t =>
			{
				var res = t.CaptureSpan(SpanName, SpanType, s =>
				{
					t.Should().NotBeNull();
					WaitHelpers.Sleep2XMinimum();
					return 42;
				});

				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method with an
		/// exception.
		/// It wraps a fake span (Thread.Sleep) with a return value that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndExceptionAndParameter()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				Action act = () =>
				{
					t.CaptureSpan(SpanName, SpanType, s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
					throw new Exception("CaptureSpan should not eat exception and continue");
				};
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		[Fact]
		public void SimpleActionWithReturnTypeAndExceptionAndParameter_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanAnd1ErrorOnSubSpan(s1 =>
			{
				Action act = () =>
				{
					s1.CaptureSpan(SpanName, SpanType, s2 =>
					{
						s2.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
					throw new Exception("CaptureSpan should not eat exception and continue");
				};
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method with an
		/// exception.
		/// It wraps a fake span (Thread.Sleep) with a return value that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndException()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				var alwaysThrow = new Random().Next(1) == 0;
				Func<int> act = () => t.CaptureSpan(SpanName, SpanType, () =>
				{
					WaitHelpers.Sleep2XMinimum();

					if (alwaysThrow) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		[Fact]
		public void SimpleActionWithReturnTypeAndException_OnSubSpan()
			=> AssertWith1TransactionAnd1SpanAnd1ErrorOnSubSpan(s =>
			{
				var alwaysThrow = new Random().Next(1) == 0;
				Func<int> act = () => s.CaptureSpan(SpanName, SpanType, () =>
				{
					WaitHelpers.Sleep2XMinimum();

					if (alwaysThrow) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Func{TResult},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) into the CaptureSpan method
		/// and it makes sure that the span is captured.
		/// </summary>
		[Fact]
		public async Task AsyncTask()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				await t.CaptureSpan(SpanName, SpanType, async () => { await WaitHelpers.Delay2XMinimum(); });
			});

		[Fact]
		public async Task AsyncTask_OnSubSpan()
			=> await AssertWith1TransactionAnd1SpanAsyncOnSubSpan(async s =>
			{
				await s.CaptureSpan(SpanName, SpanType, async () => { await WaitHelpers.Delay2XMinimum(); });
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Func{TResult},string,string)" /> method with an
		/// exception
		/// It wraps a fake async span (Task.Delay) that throws an exception into the CaptureSpan method
		/// and it makes sure that the span and the error are captured.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithException()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						await WaitHelpers.Delay2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					});
				};
				var should = await act.Should().ThrowAsync<InvalidOperationException>();
				should.WithMessage(ExceptionMessage);
			});

		[Fact]
		public async Task AsyncTaskWithExceptionOn_SubSpan()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						await WaitHelpers.Delay2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					});
				};
				var should = await act.Should().ThrowAsync<InvalidOperationException>();
				should.WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Func{TResult},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) into the CaptureSpan method with an <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span is captured and the <see cref="Action{ISpan}" /> parameter is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithParameter()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				await t.CaptureSpan(SpanName, SpanType,
					async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
					});
			});

		[Fact]
		public async Task AsyncTaskWithParameter_OnSubSpan()
			=> await AssertWith1TransactionAnd1SpanAsyncOnSubSpan(async s =>
			{
				await s.CaptureSpan(SpanName, SpanType,
					async s2 =>
					{
						s2.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
					});
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,Func{TResult},string,string)" /> method with an
		/// exception.
		/// It wraps a fake async span (Task.Delay) that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured and the <see cref="Action{ISpan}" /> parameter is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithExceptionAndParameter()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		[Fact]
		public async Task AsyncTaskWithExceptionAndParameter_OnSubSpan()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsyncOnSubSpan(async s =>
			{
				Func<Task> act = async () =>
				{
					await s.CaptureSpan(SpanName, SpanType, async s2 =>
					{
						s2.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});


		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) with a return value into the CaptureSpan method
		/// and it makes sure that the span is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnType()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				var res = await t.CaptureSpan(SpanName, SpanType, async () =>
				{
					await WaitHelpers.Delay2XMinimum();
					return 42;
				});
				res.Should().Be(42);
			});

		[Fact]
		public async Task AsyncTaskWithReturnType_OnSubSpan()
			=> await AssertWith1TransactionAnd1SpanAsyncOnSubSpan(async s =>
			{
				var res = await s.CaptureSpan(SpanName, SpanType, async () =>
				{
					await WaitHelpers.Delay2XMinimum();
					return 42;
				});
				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) with a return value into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span is captured by the agent and the return value is correct and the <see cref="ISpan" />
		/// is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndParameter()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				var res = await t.CaptureSpan(SpanName, SpanType,
					async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
						return 42;
					});

				res.Should().Be(42);
			});

		[Fact]
		public async Task AsyncTaskWithReturnTypeAndParameter_OnSubSpan()
			=> await AssertWith1TransactionAnd1SpanAsyncOnSubSpan(async s =>
			{
				var res = await s.CaptureSpan(SpanName, SpanType,
					async s2 =>
					{
						s2.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
						return 42;
					});

				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method with
		/// an exception.
		/// It wraps a fake async span (Task.Delay) with a return value that throws an exception into the CaptureSpan method with
		/// an <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		[Fact]
		public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter_OnSubSpan()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsyncOnSubSpan(async s =>
			{
				Func<Task> act = async () =>
				{
					await s.CaptureSpan(SpanName, SpanType, async s2 =>
					{
						s2.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,Func{TResult},string,string)" /> method with an
		/// exception.
		/// It wraps a fake async span (Task.Delay) with a return value that throws an exception into the CaptureSpan method
		/// and it makes sure that the span and the error are captured by the agent.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndException()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						await WaitHelpers.Delay2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		[Fact]
		public async Task AsyncTaskWithReturnTypeAndException_OnSubSpan()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsyncOnSubSpan(async s =>
			{
				Func<Task> act = async () =>
				{
					await s.CaptureSpan(SpanName, SpanType, async () =>
					{
						await WaitHelpers.Delay2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		/// <summary>
		/// Wraps a cancelled task into the CaptureSpan method and
		/// makes sure that the cancelled task is captured by the agent.
		/// </summary>
		[Fact]
		public async Task CancelledAsyncTask()
		{
			var agent = new ApmAgent(new TestAgentComponents());

			var cancellationTokenSource = new CancellationTokenSource();
			var token = cancellationTokenSource.Token;
			cancellationTokenSource.Cancel();

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						// ReSharper disable once MethodSupportsCancellation, we want to delay before we throw the exception
						await WaitHelpers.Delay2XMinimum();
						token.ThrowIfCancellationRequested();
					});
				};
				await act.Should().ThrowAsync<OperationCanceledException>();
			});
		}

		/// <summary>
		/// Creates a custom span and adds a label to it.
		/// Makes sure that the label is stored on Span.Context.
		/// </summary>
		[Fact]
		public void LabelsOnSpan()
		{
			var payloadSender = AssertWith1TransactionAnd1Span(
				t =>
				{
					t.CaptureSpan(SpanName, SpanType, span =>
					{
						WaitHelpers.Sleep2XMinimum();
						span.SetLabel("foo", "bar");
					});
				});

			//According to the Intake API labels are stored on the Context (and not on Spans.Labels directly).
			payloadSender.SpansOnFirstTransaction[0].Context.InternalLabels.Value.InnerDictionary["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Creates a custom async span and adds a label to it.
		/// Makes sure that the label is stored on Span.Context.
		/// </summary>
		[Fact]
		public async Task LabelsOnSpanAsync()
		{
			var payloadSender = await AssertWith1TransactionAnd1SpanAsync(
				async t =>
				{
					await t.CaptureSpan(SpanName, SpanType, async span =>
					{
						await WaitHelpers.Delay2XMinimum();
						span.SetLabel("foo", "bar");
					});
				});

			//According to the Intake API labels are stored on the Context (and not on Spans.Labels directly).
			payloadSender.SpansOnFirstTransaction[0].Context.InternalLabels.Value.MergedDictionary["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Creates a custom async span that ends with an error and adds a label to it.
		/// Makes sure that the label is stored on Span.Context.
		/// </summary>
		[Fact]
		public async Task LabelsOnSpanAsyncError()
		{
			var payloadSender = await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async span =>
					{
						await WaitHelpers.Delay2XMinimum();
						span.SetLabel("foo", "bar");

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

			//According to the Intake API labels are stored on the Context (and not on Spans.Labels directly).
			payloadSender.SpansOnFirstTransaction[0].Context.InternalLabels.Value.MergedDictionary["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Creates 1 span with db information on it and creates a 2. span with http information on it.
		/// Makes sure the db and http info is captured on the span's context.
		/// </summary>
		[Fact]
		public void FillSpanContext()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
			{
				WaitHelpers.SleepMinimum();
				t.CaptureSpan("SampleSpan1", "SampleSpanType",
					span => { span.Context.Http = new Http { Url = "http://mysite.com", Method = "GET", StatusCode = 200 }; });

				t.CaptureSpan("SampleSpan2", "SampleSpanType",
					span =>
					{
						span.Context.Db = new Database { Statement = "Select * from MyTable", Type = Database.TypeSql, Instance = "MyInstance" };
					});
			});

			payloadSender.Spans[0].Name.Should().Be("SampleSpan1");
			payloadSender.Spans[0].Context.Http.Url.Should().Be("http://mysite.com");
			payloadSender.Spans[0].Context.Http.Method.Should().Be("GET");
			payloadSender.Spans[0].Context.Http.StatusCode.Should().Be(200);
			payloadSender.Spans[0].Context.Destination.Address.Should().Be("mysite.com");
			payloadSender.Spans[0].Context.Destination.Port.Should().Be(UrlUtilsTests.DefaultHttpPort);

			payloadSender.Spans[1].Name.Should().Be("SampleSpan2");
			payloadSender.Spans[1].Context.Db.Statement.Should().Be("Select * from MyTable");
			payloadSender.Spans[1].Context.Db.Type.Should().Be(Database.TypeSql);
			payloadSender.Spans[1].Context.Db.Instance.Should().Be("MyInstance");
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 async span and 1 error
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAnd1ErrorAnd1SpanAsync(Func<ITransaction, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				await WaitHelpers.DelayMinimum();
				await func(t);
			});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.Exception.Message.Should().Be(ExceptionMessage);

			return payloadSender;
		}

		private async Task<MockPayloadSender> AssertWith1TransactionAnd1ErrorAnd1SpanAsyncOnSubSpan(Func<ISpan, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				await WaitHelpers.DelayMinimum();

				await t.CaptureSpan("TestSpan", "TestSpanType", async s =>
				{
					await WaitHelpers.DelayMinimum();
					await func(s);
				});
			});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.Exception.Message.Should().Be(ExceptionMessage);

			var orderedSpans = payloadSender.Spans.OrderBy(n => n.Timestamp).ToList();

			var firstSpan = orderedSpans.First();
			var innerSpan = orderedSpans.Last();

			firstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.ParentId.Should().Be(firstSpan.Id);

			firstSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);

			return payloadSender;
		}


		/// <summary>
		/// Asserts on 1 transaction with 1 async Span
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAnd1SpanAsync(Func<ITransaction, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				await WaitHelpers.DelayMinimum();
				await func(t);
			});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			return payloadSender;
		}

		private async Task<MockPayloadSender> AssertWith1TransactionAnd1SpanAsyncOnSubSpan(Func<ISpan, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				await WaitHelpers.DelayMinimum();

				await t.CaptureSpan("SubSpan", "SubSpanType", async s =>
				{
					await WaitHelpers.DelayMinimum();
					await func(s);
				});
			});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			var orderedSpans = payloadSender.Spans.OrderBy(n => n.Timestamp).ToList();

			var firstSpan = orderedSpans.First();
			var innerSpan = orderedSpans.Last();

			firstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.ParentId.Should().Be(firstSpan.Id);

			firstSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 span
		/// </summary>
		private MockPayloadSender AssertWith1TransactionAnd1Span(Action<ITransaction> action)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
			{
				WaitHelpers.SleepMinimum();
				action(t);
			});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			return payloadSender;
		}

		private void AssertWith1TransactionAnd1SpanOnSubSpan(Action<ISpan> action)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			WaitHelpers.SleepMinimum();
			agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
			{
				WaitHelpers.SleepMinimum();
				t.CaptureSpan("aa", "bb", s => //TODO Name
				{
					WaitHelpers.SleepMinimum();
					action(s);
				});
			});

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			var orderedSpans = payloadSender.Spans.OrderBy(n => n.Timestamp).ToList();

			var firstSpan = orderedSpans.First();
			var innerSpan = orderedSpans.Last();

			firstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.ParentId.Should().Be(firstSpan.Id);

			firstSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 span and 1 error
		/// </summary>
		private void AssertWith1TransactionAnd1SpanAnd1Error(Action<ITransaction> action)
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
				{
					WaitHelpers.SleepMinimum();
					action(t);
				});
			}

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.Exception.Message.Should().Be(ExceptionMessage);
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 span and 1 error
		/// </summary>
		private void AssertWith1TransactionAnd1SpanAnd1ErrorOnSubSpan(Action<ISpan> action)
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				WaitHelpers.SleepMinimum();
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
				{
					WaitHelpers.SleepMinimum();

					t.CaptureSpan("aa", "bb", s =>
					{
						WaitHelpers.SleepMinimum();
						action(s);
					});
				});
			}

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.Exception.Message.Should().Be(ExceptionMessage);

			var orderedSpans = payloadSender.Spans.OrderBy(n => n.Timestamp).ToList();

			var firstSpan = orderedSpans.First();
			var innerSpan = orderedSpans.Last();

			firstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.ParentId.Should().Be(firstSpan.Id);

			firstSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
		}
	}
}
