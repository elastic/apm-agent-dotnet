using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Elastic.Apm.Tests.MockApmServer
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
			_logger = mockApmServer.Logger.Scoped(nameof(IntakeV2EventsController));

			_logger.Debug()?.Log("Constructed with mock APM Server: {MockApmServer}", _mockApmServer);
		}

		[HttpPost]
		public async Task<IActionResult> Post()
		{
			_logger.Debug()?.Log("Received request with content length: {ContentLength}", Request.ContentLength);

			int numberOfObjects;

			try
			{
				numberOfObjects = await ParsePayload();
			}
			catch (ArgumentException ex)
			{
				_mockApmServer.ReceivedData.InvalidPayloadErrors.Add(ex.Message);
				return BadRequest(ex.Message);
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
					ParsePayloadLine(line);
					++numberOfParsedLines;
				}
			}

			return numberOfParsedLines;
		}

		private void ParsePayloadLine(string line)
		{
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

//			Thread.Sleep(5000);

			var numberOfObjects = 0;

			if (lineDto.Error != null)
			{
				_logger.Debug()?.Log("Successfully parsed Error object: {ErrorDto}, input line: `{PayloadLine}'", lineDto.Error, line);
				++numberOfObjects;
				_mockApmServer.ReceivedData.Errors.Add(lineDto.Error);
			}

			if (lineDto.Metadata != null)
			{
				_logger.Debug()?.Log("Successfully parsed Metadata object: {MetadataDto}, input line: `{PayloadLine}'", lineDto.Metadata, line);
				++numberOfObjects;
				_mockApmServer.ReceivedData.Metadata.Add(lineDto.Metadata);
			}

			if (lineDto.MetricSet != null)
			{
				_logger.Debug()?.Log("Successfully parsed MetricSet object: {MetricSetDto}, input line: `{PayloadLine}'", lineDto.MetricSet, line);
				++numberOfObjects;
				_mockApmServer.ReceivedData.Metrics.Add(lineDto.MetricSet);
			}

			if (lineDto.Span != null)
			{
				_logger.Debug()?.Log("Successfully parsed Span object: {SpanDto}, input line: `{PayloadLine}'", lineDto.Span, line);
				++numberOfObjects;
				_mockApmServer.ReceivedData.Spans.Add(lineDto.Span);
			}

			if (lineDto.Transaction != null)
			{
				_logger.Debug()?.Log("Successfully parsed Transaction object: {TransactionDto}, input line: `{PayloadLine}'", lineDto.Transaction, line);
				++numberOfObjects;
				_mockApmServer.ReceivedData.Transactions.Add(lineDto.Transaction);
			}

			if (numberOfObjects == 0)
				throw new ArgumentException($"Payload line does not contain any object: `{line}'");

			if (numberOfObjects > 1)
				throw new ArgumentException($"Payload line contains more than one object ({numberOfObjects} objects): `{line}'");
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
