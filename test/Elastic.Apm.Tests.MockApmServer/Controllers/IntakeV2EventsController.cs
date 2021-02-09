// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Specification;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using Xunit.Sdk;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Elastic.Apm.Tests.MockApmServer.Controllers
{
	[Route("intake/v2/events")]
	[ApiController]
	public class IntakeV2EventsController : ControllerBase
	{
		private static readonly ConcurrentDictionary<Type, Lazy<Task<JsonSchema>>> Schemata =
			new ConcurrentDictionary<Type, Lazy<Task<JsonSchema>>>();

		private const string ExpectedContentType = "application/x-ndjson; charset=utf-8";
		private const string ThisClassName = nameof(IntakeV2EventsController);
		private readonly IApmLogger _logger;
		private readonly MockApmServer _mockApmServer;
		private readonly Validator _validator;

		public IntakeV2EventsController(MockApmServer mockApmServer, Validator validator)
		{
			_mockApmServer = mockApmServer;
			_validator = validator;
			_logger = mockApmServer.InternalLogger.Scoped(ThisClassName + "#" + RuntimeHelpers.GetHashCode(this).ToString("X"));
			_logger.Debug()?.Log("Constructed with mock APM Server: {MockApmServer}", _mockApmServer);
		}

		[HttpPost]
		public Task<IActionResult> Post() => _mockApmServer.DoUnderLock(PostImpl);

		private async Task<IActionResult> PostImpl()
		{
			_logger.Debug()
				?.Log("Received request with content length: {ContentLength}."
					+ " Current thread: {ThreadDesc}."
					, Request.ContentLength, DbgUtils.CurrentThreadDesc);

			int numberOfObjects;

			try
			{
				numberOfObjects = await ParsePayload();
			}
			catch (ArgumentException ex)
			{
				_mockApmServer.AddInvalidPayload(ex.ToString());
				return BadRequest(ex.ToString());
			}

			_logger.Debug()?.Log("Successfully parsed {numberOfObjects} objects", numberOfObjects);

			return Ok($"Successfully processed request with Content-Length: {Request.ContentLength} and number of objects: {numberOfObjects}");
		}

		private async Task<int> ParsePayload()
		{
			if (Request.ContentType.ToLowerInvariant() != ExpectedContentType)
			{
				throw new ArgumentException(
					$"Request has unexpected Content-Type. Expected: `{ExpectedContentType}', actual: `{Request.ContentType}'");
			}

			string requestBody;
			using (var reader = new StreamReader(Request.Body, Encoding.UTF8)) requestBody = await reader.ReadToEndAsync();

			var numberOfParsedLines = 0;
			using (var sr = new StringReader(requestBody))
			{
				string line;
				while ((line = await sr.ReadLineAsync()) != null)
				{
					await ParsePayloadLineAndAddToReceivedData(line);
					++numberOfParsedLines;
				}
			}

			return numberOfParsedLines;
		}

		private async Task ParsePayloadLineAndAddToReceivedData(string line)
		{
			var foundDto = false;

			var payload = JsonConvert.DeserializeObject<PayloadLineDto>(
				line,
				new JsonSerializerSettings
				{
					MissingMemberHandling = MissingMemberHandling.Error,
					Error = (_, errorEventArgs) =>
					{
						_logger.Error()
							?.Log("Failed to parse payload line as JSON. Error: {PayloadParsingErrorMessage}, line: `{PayloadLine}'",
								errorEventArgs.ErrorContext.Error.Message, line);
						throw new ArgumentException(errorEventArgs.ErrorContext.Error.Message);
					}
				});

			await HandleParsed(nameof(payload.Error), payload.Error, _mockApmServer.ReceivedData.Errors, _mockApmServer.AddError);
			await HandleParsed(nameof(payload.Metadata), payload.Metadata, _mockApmServer.ReceivedData.Metadata, _mockApmServer.AddMetadata);
			await HandleParsed(nameof(payload.MetricSet), payload.MetricSet, _mockApmServer.ReceivedData.Metrics, _mockApmServer.AddMetricSet);
			await HandleParsed(nameof(payload.Span), payload.Span, _mockApmServer.ReceivedData.Spans, _mockApmServer.AddSpan);
			await HandleParsed(nameof(payload.Transaction), payload.Transaction, _mockApmServer.ReceivedData.Transactions, _mockApmServer.AddTransaction);

			foundDto.Should().BeTrue($"There should be exactly one object per line: `{line}'");

			async Task HandleParsed<TDto>(string dtoType, TDto dto, ImmutableList<TDto> accumulatingList, Action<TDto> action) where TDto : IDto
			{
				if (dto == null) return;

				foundDto.Should().BeFalse($"There should be exactly one object per line: `{line}'");
				foundDto = true;

				try
				{
					// validate the DTO
					dto.AssertValid();

					// validate against the json schema
					var schema = await Schemata.GetOrAdd(typeof(TDto), (t, v) =>
						{
							var specificationId = v.GetSpecificationIdForType(t);
							return new Lazy<Task<JsonSchema>>(v.LoadSchemaAsync(specificationId));
						}, _validator)
						.Value;

					var jObject = JObject.Parse(line);
					var value = jObject.GetValue(dtoType, StringComparison.OrdinalIgnoreCase);
					var validationErrors = schema.Validate(value);
					validationErrors.Should().BeEmpty();
				}
				catch (Exception ex)
				{
					if (ex is XunitException)
					{
						_logger.Error()
							?.LogException(ex,
								"{EventDtoType} #{EventDtoSeqNumber} was parsed successfully but it didn't pass semantic verification.\n" +
								"Input line (pretty formatted):\n".Indent() + "{EventDtoJson}\n" +
								"Parsed object:\n".Indent() + "{EventDtoParsed}",
								dtoType, accumulatingList.Count + 1,
								line.PrettyFormat().Indent(2), dto.ToString().Indent(2));
						_mockApmServer.AddInvalidPayload(ex.ToString());
						return;
					}

					_logger.Error()
						?.LogException(ex,
							"{EventDtoType} #{EventDtoSeqNumber} was parsed successfully but exception when validating against JSON schema.\n" +
							"Input line (pretty formatted):\n".Indent() + "{EventDtoJson}\n" +
							"Parsed object:\n".Indent() + "{EventDtoParsed}",
							dtoType, accumulatingList.Count + 1,
							line.PrettyFormat().Indent(2), dto.ToString().Indent(2));

					throw;
				}

				_logger.Debug()
					?.Log("Successfully parsed and verified {EventDtoType} #{EventDtoSeqNumber}.\n" +
						"Input line (pretty formatted):\n".Indent() + "{EventDtoJson}\n" +
						"Parsed object:\n".Indent() + "{EventDtoParsed}",
						dtoType, accumulatingList.Count + 1,
						line.PrettyFormat().Indent(2), dto.ToString().Indent(2));

				action(dto);
			}
		}

		private class PayloadLineDto
		{
			public ErrorDto Error { get; set; }

			public MetadataDto Metadata { get; set; }

			[JsonProperty("metricset")]
			public MetricSetDto MetricSet { get; set; }

			public SpanDto Span { get; set; }

			public TransactionDto Transaction { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(PayloadLineDto))
			{
				{ nameof(Error), Error },
				{ nameof(Metadata), Metadata },
				{ nameof(MetricSet), MetricSet },
				{ nameof(Span), Span },
				{ nameof(Transaction), Transaction }
			}.ToString();
		}
	}
}
