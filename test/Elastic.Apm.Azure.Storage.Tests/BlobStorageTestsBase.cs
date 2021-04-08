// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Storage.Tests
{
	public abstract class BlobStorageTestsBase
	{
		private readonly MockPayloadSender _sender;
		protected IApmAgent Agent { get; }
		protected AzureStorageTestEnvironment Environment { get; }

		protected BlobStorageTestsBase(AzureStorageTestEnvironment environment, ITestOutputHelper output)
		{
			Environment = environment;
			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			Agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			Agent.Subscribe(new AzureBlobStorageDiagnosticsSubscriber());
		}

		protected void AssertSpan(string action, string resource, int spanIndex = 0)
		{
			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCountGreaterOrEqualTo(1);
			var span = _sender.Spans[spanIndex];

			span.Name.Should().Be($"{AzureBlobStorage.SpanName} {action} {resource}");
			span.Type.Should().Be(ApiConstants.TypeStorage);
			span.Subtype.Should().Be(AzureBlobStorage.SubType);
			span.Action.Should().Be(action);
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(Environment.StorageAccountConnectionStringProperties.BlobUrl);
			destination.Service.Name.Should().Be(AzureBlobStorage.SubType);
			destination.Service.Resource.Should().Be($"{AzureBlobStorage.SubType}/{resource}");
			destination.Service.Type.Should().Be(ApiConstants.TypeStorage);
		}

	}
}
