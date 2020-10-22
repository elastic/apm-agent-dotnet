// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class Error : IError
	{
		public Error(CapturedException capturedException, Transaction transaction, string parentId, IApmLogger loggerArg)
		{
			Timestamp = TimeUtils.TimestampNow();
			Id = RandomGenerator.GenerateRandomBytesAsString(new byte[16]);

			Exception = capturedException;

			TraceId = transaction.TraceId;
			TransactionId = transaction.Id;
			ParentId = parentId;
			Transaction = new TransactionData(transaction.IsSampled, transaction.Type);

			if (transaction.IsSampled) Context = transaction.Context;

			IApmLogger logger = loggerArg?.Scoped($"{nameof(Error)}.{Id}");
			logger.Trace()
				?.Log("New Error instance created: {Error}. Time: {Time} (as timestamp: {Timestamp})",
					this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
		}

		// This constructor is meant for serialization
		[JsonConstructor]
		private Error(string culprit, CapturedException capturedException, string id, string parentId, long timestamp, string traceId,
			string transactionId, TransactionData transaction
		)
		{
			Culprit = culprit;
			Exception = capturedException;
			Id = id;
			ParentId = parentId;
			Timestamp = timestamp;
			TraceId = traceId;
			TransactionId = transactionId;
			Transaction = transaction;
		}

		/// <summary>
		/// <seealso cref="ShouldSerializeContext" />
		/// </summary>
		public Context Context { get; set; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		public string Culprit { get; set; }

		public CapturedException Exception { get; set; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		public string Id { get; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public TransactionData Transaction { get; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="Context" /> because context should be serialized only when the transaction
		/// is sampled.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeContext() => Transaction.IsSampled;

		public override string ToString() => new ToStringBuilder(nameof(Error))
		{
			{ nameof(Id), Id }, { nameof(TraceId), TraceId }, { nameof(ParentId), ParentId }, { nameof(TransactionId), TransactionId }
		}.ToString();

		public class TransactionData
		{
			[JsonConstructor]
			internal TransactionData(bool isSampled, string type)
			{
				IsSampled = isSampled;
				Type = type;
			}

			[JsonProperty("sampled")]
			public bool IsSampled { get; }

			[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
			public string Type { get; }

			public override string ToString() =>
				new ToStringBuilder(nameof(TransactionData)) { { "IsSampled", IsSampled }, { "Type", Type } }.ToString();
		}
	}
}
