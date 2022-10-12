// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class ExitSpanTests
{
	/// <summary>
	/// Tests a fully filled DB Span which contains type, subtype and the Db fields
	/// </summary>
	[Fact]
	public void DbSpan()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("Select From MyTable", ApiConstants.TypeDb, s =>
				{
					s.Context.Db = new Database { Instance = "myInstance", Statement = "Select * From MyTable", Type = Database.TypeSql };
				}, ApiConstants.SubtypeMssql, isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be(ApiConstants.SubtypeMssql + "/myInstance");

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().Be("myInstance");
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeMssql);
	}

	/// <summary>
	/// Tests a db span that has only db type, but no subtype and db instance.
	/// Makes sure `Service.Target.*` and `Destination.Service.Resource.*` falls back to type
	/// </summary>
	[Fact]
	public void DbSpanWithOnlyType()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("Select From MyTable", ApiConstants.TypeDb, s =>
				{
					s.Context.Db = new Database { Type = Database.TypeSql };
				}, isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be(ApiConstants.TypeDb);

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().BeNullOrEmpty();
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be(ApiConstants.TypeDb);
	}

	/// <summary>
	/// Tests a db span that has subtype, but no db.insrance
	/// Makes sure `Service.Target.*` and `Destination.Service.Resource.*` uses the subtype
	/// </summary>
	[Fact]
	public void DbSpanWithSubTypeButNoInstance()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("Select From MyTable", ApiConstants.TypeDb, s =>
				{
					s.Context.Db = new Database { Type = Database.TypeSql };
				}, subType: ApiConstants.SubtypeMssql, isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be(ApiConstants.SubtypeMssql);

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().BeNullOrEmpty();
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeMssql);
	}

	[Fact]
	public void HttpExitSpanToElastic()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("GET elastic.co", ApiConstants.TypeExternal, s =>
				{
					s.Context.Http = new Http() { Method = "GET", StatusCode = 200 };
					s.Context.Http.SetUrl(new Uri("https://elastic.co"));
				}, subType: ApiConstants.SubtypeHttp, isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be("elastic.co:443");

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().Be("elastic.co:443");
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeHttp);
	}

	[Fact]
	public void HttpExitSpanToHostPort80()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("GET host", ApiConstants.TypeExternal, s =>
				{
					s.Context.Http = new Http() { Method = "GET", StatusCode = 200 };
					s.Context.Http.SetUrl(new Uri("http://host:80"));
				}, subType: ApiConstants.SubtypeHttp, isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be("host:80");

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().Be("host:80");
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeHttp);
	}

	[Fact]
	public void MessagingExitSpanWithoutQueue()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("RabbitMq", ApiConstants.TypeMessaging, s =>
				{
					s.Context.Message = new Message { Age = new Age { Ms = 1 }, Body = "foo", RoutingKey = "bar" };
				}, subType: "rabbitmq", isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be("rabbitmq");

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().BeNullOrEmpty();
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be("rabbitmq");
	}

	[Fact]
	public void MessagingExitSpanWithQueue()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("RabbitMq", ApiConstants.TypeMessaging, s =>
				{
					s.Context.Message = new Message { Queue = new Queue { Name = "my-queue" } };
				}, subType: "rabbitmq", isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be("rabbitmq/my-queue");

		payloadSender.FirstSpan.Context.Service.Target.Name.Should().Be("my-queue");
		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be("rabbitmq");
	}

	/// <summary>
	/// Makes sure that setting <see cref="SpanContext.Service"/> has higher precedence
	/// than the logic to infer those values automatically.
	/// </summary>
	[Fact]
	public void ManuallySetTargetHasHigherPrecedence()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				   configuration: new MockConfiguration(spanCompressionEnabled: "false"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.CaptureSpan("Select From MyTable", ApiConstants.TypeDb, s =>
				{
					// Fill db fields - this is used for the logic to manually infer Service.Target.*
					s.Context.Db = new Database { Instance = "myInstance", Statement = "Select * From MyTable", Type = Database.TypeSql };

					// Manually set Service.Target to a custom value different from what the agent would infer
					s.Context.Service = new SpanService(new Target("foo", "bar"));
				}, ApiConstants.SubtypeMssql, isExitSpan: true);
			});
		}

		payloadSender.FirstSpan.Context.Service.Target.Type.Should().Be("foo");
		payloadSender.FirstSpan.Context.Service.Target.Name.Should().Be("bar");
	}
}
