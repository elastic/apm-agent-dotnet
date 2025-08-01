// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Model
{
	internal class Error : IError
	{
		[JsonIgnore]
		internal IConfiguration Configuration { get; }

		public Error(CapturedException capturedException, Transaction transaction, string parentId, IApmLogger loggerArg, Dictionary<string, Label> labels = null) :
			this(transaction, parentId, loggerArg, labels) => Exception = capturedException;

		public Error(ErrorLog errorLog, Transaction transaction, string parentId, IApmLogger loggerArg, Dictionary<string, Label> labels = null) :
			this(transaction, parentId, loggerArg, labels)
				=> Log = errorLog;

		private Error(Transaction transaction, string parentId, IApmLogger logger, Dictionary<string, Label> labels = null)
		{
			Timestamp = TimeUtils.TimestampNow();
			Id = RandomGenerator.GenerateRandomBytesAsString(new byte[16]);

			if (transaction != null)
			{
				TraceId = transaction.TraceId;
				TransactionId = transaction.Id;
				Transaction = new TransactionData(transaction.Name, transaction.IsSampled, transaction.Type);
				Configuration = transaction.Configuration;
			}

			ParentId = parentId;

			if (transaction is { IsSampled: true })
			{
				Context = transaction.Context.DeepCopy();

				if (labels != null)
				{
					foreach (var item in labels)
						Context.InternalLabels.Value.InnerDictionary[item.Key] = item.Value;
				}
			}

			CheckAndCaptureBaggage(transaction);

			logger?.Scoped(nameof(Error))?.Trace()?.Log("New Error instance created: {Error}. Time: {Time} (as timestamp: {Timestamp})",
					this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
		}

		private void CheckAndCaptureBaggage(Transaction transaction)
		{
			if (Activity.Current == null || !Activity.Current.Baggage.Any())
				return;

			//if context was not set prior we set it now to ensure we capture baggage for errors
			//occuring during unsampled transactions
			Context ??= transaction?.Context.DeepCopy();

			foreach (var baggage in Activity.Current.Baggage)
			{
				if (!WildcardMatcher.IsAnyMatch(Configuration.BaggageToAttach, baggage.Key))
					continue;

				// The context is created only if there is a baggage value to insert.
				Context ??= new Context();

				var newKey = $"baggage.{baggage.Key}";
				var labels = Context.InternalLabels.Value;
				labels[newKey] = baggage.Value;
			}
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

		[MaxLength]
		public string Culprit { get; set; }

		public CapturedException Exception { get; set; }

		[MaxLength]
		public string Id { get; }

		public ErrorLog Log { get; set; }

		[MaxLength]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; }

		[MaxLength]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public TransactionData Transaction { get; }

		[MaxLength]
		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="Context" /> because context should be serialized only when the transaction
		/// is sampled.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeContext() => Transaction != null && Transaction.IsSampled;

		public override string ToString() => new ToStringBuilder(nameof(Error))
		{
			{ nameof(Id), Id }, { nameof(TraceId), TraceId }, { nameof(ParentId), ParentId }, { nameof(TransactionId), TransactionId }
		}.ToString();

		public class TransactionData
		{
			[JsonConstructor]
			internal TransactionData(string name, bool isSampled, string type)
			{
				Name = name;
				IsSampled = isSampled;
				Type = type;
			}

			/// <summary>
			/// IsSampled indicates whether or not the full information for a transaction is captured. If a transaction is unsampled no spans and less context information will be reported.
			/// </summary>
			[JsonProperty("sampled")]
			public bool IsSampled { get; }

			/// <summary>
			/// Name is the generic designation of a transaction in the scope of a single service, eg: 'GET /users/:id'.
			/// </summary>
			[MaxLength]
			public string Name { get; }

			/// <summary>
			/// Type expresses the correlated transaction's type as keyword that has specific relevance within the service's domain, eg: 'request', 'backgroundjob'.
			/// </summary>
			[MaxLength]
			public string Type { get; }

			public override string ToString() =>
				new ToStringBuilder(nameof(TransactionData)) { { "IsSampled", IsSampled }, { "Type", Type } }.ToString();
		}
	}
}
