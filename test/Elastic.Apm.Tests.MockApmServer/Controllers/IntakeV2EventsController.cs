using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Xunit.Sdk;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Elastic.Apm.Tests.MockApmServer.Controllers
{
	[Route("intake/v2/events")]
	[ApiController]
	public class IntakeV2EventsController : ControllerBase
	{
		private const string ExpectedContentType = "application/x-ndjson; charset=utf-8";
		private readonly IApmLogger _logger;

		private readonly MockApmServer _mockApmServer;

		public IntakeV2EventsController(MockApmServer mockApmServer)
		{
			_mockApmServer = mockApmServer;
			_logger = mockApmServer.Logger.Scoped(nameof(IntakeV2EventsController) + "#" + RuntimeHelpers.GetHashCode(this).ToString("X"));

			_logger.Debug()?.Log("Constructed with mock APM Server: {MockApmServer}", _mockApmServer);
		}

		[HttpPost]
		public Task<IActionResult> Post() => _mockApmServer.DoUnderLock(PostImpl);

		private async Task<IActionResult> PostImpl()
		{
			_logger.Debug()?.Log("Received request with content length: {ContentLength}."
				+ " Current thread: name `{ThreadName}', managed ID: {ThreadManagedId}."
				, Request.ContentLength
				, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId);

			int numberOfObjects;

			try
			{
				numberOfObjects = await ParsePayload();
			}
			catch (ArgumentException ex)
			{
				_mockApmServer.ReceivedData.InvalidPayloadErrors = _mockApmServer.ReceivedData.InvalidPayloadErrors.Add(ex.ToString());
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
				while ((line = sr.ReadLine()) != null)
				{
					ParsePayloadLineAndAddToReceivedData(line);
					++numberOfParsedLines;
				}
			}

			return numberOfParsedLines;
		}

		private void ParsePayloadLineAndAddToReceivedData(string line)
		{
			var foundDto = false;

			var lineDto = JsonConvert.DeserializeObject<PayloadLineDto>(
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

			_mockApmServer.ReceivedData.Errors = HandleParsed("Error", lineDto.Error, _mockApmServer.ReceivedData.Errors);
			_mockApmServer.ReceivedData.Metadata = HandleParsed("Metadata", lineDto.Metadata, _mockApmServer.ReceivedData.Metadata);
			_mockApmServer.ReceivedData.Metrics = HandleParsed("MetricSet", lineDto.MetricSet, _mockApmServer.ReceivedData.Metrics);
			_mockApmServer.ReceivedData.Spans = HandleParsed("Span", lineDto.Span, _mockApmServer.ReceivedData.Spans);
			_mockApmServer.ReceivedData.Transactions = HandleParsed("Transaction", lineDto.Transaction, _mockApmServer.ReceivedData.Transactions);

			foundDto.Should().BeTrue($"There should be exactly one object per line: `{line}'");

			ImmutableList<TDto> HandleParsed<TDto>(string dtoType, TDto dto, ImmutableList<TDto> accumulatingList) where TDto : IDto
			{
				if (dto == null) return accumulatingList;

				foundDto.Should().BeFalse($"There should be exactly one object per line: `{line}'");
				foundDto = true;

				try
				{
					dto.AssertValid();
				}
				catch (XunitException ex)
				{
					_logger.Error()
						?.LogException(ex,
							"{EventDtoType} #{EventDtoSeqNumber} was parsed successfully but it didn't pass semantic verification.\n" +
							TextUtils.Indent("Input line (pretty formatted):\n") + "{EventDtoJson}\n" +
							TextUtils.Indent("Parsed object:\n") + "{EventDtoParsed}",
							dtoType, accumulatingList.Count + 1,
							TextUtils.Indent(JsonUtils.PrettyFormat(line), 2), TextUtils.Indent(dto.ToString(), 2));
					_mockApmServer.ReceivedData.InvalidPayloadErrors = _mockApmServer.ReceivedData.InvalidPayloadErrors.Add(ex.ToString());
					return accumulatingList;
				}

				_logger.Debug()
					?.Log("Successfully parsed and verified {EventDtoType} #{EventDtoSeqNumber}.\n" +
						TextUtils.Indent("Input line (pretty formatted):\n") + "{EventDtoJson}\n" +
						TextUtils.Indent("Parsed object:\n") + "{EventDtoParsed}",
						dtoType, accumulatingList.Count + 1,
						TextUtils.Indent(JsonUtils.PrettyFormat(line), 2), TextUtils.Indent(dto.ToString(), 2));

				return accumulatingList.Add(dto);
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
				{ "Error", Error },
				{ "Metadata", Metadata },
				{ "MetricSet", MetricSet },
				{ "Span", Span },
				{ "Transaction", Transaction },
			}.ToString();
		}
	}
}
