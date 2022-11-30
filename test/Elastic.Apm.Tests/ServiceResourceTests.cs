// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using TestData;
using Xunit;
using Http = Elastic.Apm.Api.Http;
using Message = Elastic.Apm.Api.Message;
using Queue = Elastic.Apm.Api.Queue;
using Target = Elastic.Apm.Api.Target;

namespace Elastic.Apm.Tests
{
	public class ServiceResourceTest : IDisposable
	{
		private readonly ITransaction _transaction;

		public ServiceResourceTest() => _transaction = Agent.Tracer.StartTransaction("root", "type");

		public void Dispose() => _transaction.End();

		[Theory]
		[JsonFileData("./TestResources/json-specs/service_resource_inference.json", typeof(Input))]
		public void TestServiceResourceInference(Input input)
		{
			var span = CreateSpan(input.Span);
			// auto-inference happens now
			span.End();

			var targetType = span.Context?.Service?.Target?.Type;
			if (input.ExpectedServiceTarget?.Type != null)
				targetType.Should().Be(input.ExpectedServiceTarget.Type, input.FailureMessage);
			else
				targetType.Should().BeNull(input.FailureMessage);

			var targetName = span.Context?.Service?.Target?.Name;
			if (input.ExpectedServiceTarget?.Name != null)
			{
				targetName.Should().Be(input.ExpectedServiceTarget.Name, input.FailureMessage);
			}
			else
				targetName.Should().BeNull(input.FailureMessage);

			var resource = span.Context?.Destination?.Service?.Resource;
			if (input.ExpectedResource != null)
			{
				resource.Should().Be(input.ExpectedResource, input.FailureMessage);
			}
			else
				resource.Should().BeNull(input.FailureMessage);
		}

		private ISpan CreateSpan(Span testDataSpan)
		{
			var span = _transaction.StartSpan("name", testDataSpan.Type, testDataSpan.SubType, isExitSpan: testDataSpan.Exit);

			if (testDataSpan.Context?.Db != null)
			{
				span.Context.Db = new Database
				{
					Type = testDataSpan.Context.Db.Type, Instance = testDataSpan.Context.Db.Instance
				};
			}

			if (testDataSpan.Context?.Message != null)
			{
				span.Context.Message = new Message
				{
					Body = testDataSpan.Context.Message.Body,
					Queue = new Queue { Name = testDataSpan.Context.Message?.Queue?.Name }
				};
			}

			if (testDataSpan.Context?.Http != null)
			{
				span.Context.Http = new Http { Url = testDataSpan.Context.Http.Url };
			}

			if (testDataSpan.Context?.Service != null)
			{
				span.Context.Service = new SpanService(new Target(testDataSpan.Context.Service.Target.Type,
					testDataSpan.Context.Service.Target.Name));
			}

			return span;
		}
	}
}

namespace TestData
{
	public class Input
	{
		public Span Span { get; set; }
		[JsonProperty("expected_resource")] public string ExpectedResource { get; set; }

		[JsonProperty("expected_service_target")]
		public ExpectedServiceTarget ExpectedServiceTarget { get; set; }

		[JsonProperty("failure_message")] public string FailureMessage { get; set; }
	}

	public class Context
	{
		public Service Service { get; set; }
		public Db Db { get; set; }
		public Http Http { get; set; }
		public Message Message { get; set; }
	}

	public class Db
	{
		public string Instance { get; set; }
		public string Type { get; set; }
	}

	public class ExpectedServiceTarget
	{
		public string Type { get; set; }
		public string Name { get; set; }
	}

	public class Http
	{
		public string Url { get; set; }
	}

	public class Message
	{
		public string Body { get; set; }
		public Queue Queue { get; set; }
	}

	public class Queue
	{
		public string Name { get; set; }
	}

	public class Service
	{
		public Target Target { get; set; }
	}

	public class Span
	{
		public bool Exit { get; set; }
		public string Type { get; set; }
		public string SubType { get; set; }
		public Context Context { get; set; }
	}

	public class Target
	{
		public string Type { get; set; }
		public string Name { get; set; }
	}
}
