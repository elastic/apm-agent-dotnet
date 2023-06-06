// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Report.Serialization;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricSetConverter))]
	internal class MetricSet : IMetricSet
	{
		public MetricSet(long timestamp, IEnumerable<MetricSample> samples)
			=> (Timestamp, Samples) = (timestamp, samples);
		public MetricSet(IEnumerable<MetricSample> samples)
			=> Samples = samples;

		/// <inheritdoc />
		public IEnumerable<MetricSample> Samples { get; set; }

		/// <inheritdoc />
		public long Timestamp { get; set; }

		public TransactionInfo Transaction { get; set; }

		public SpanInfo Span { get; set; }
	}

	public class TransactionInfo : IEquatable<TransactionInfo>
	{
		[MaxLength]
		public string Name { get; set; }
		[MaxLength]
		public string Type { get; set; }

		public bool Equals(TransactionInfo other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return Name == other.Name && Type == other.Type;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			return Equals((TransactionInfo)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
			}
		}

		public static bool operator ==(TransactionInfo left, TransactionInfo right) => Equals(left, right);

		public static bool operator !=(TransactionInfo left, TransactionInfo right) => !Equals(left, right);
	}

	public class SpanInfo : IEquatable<SpanInfo>
	{
		[MaxLength]
		public string Type { get; set; }
		[MaxLength]
		public string SubType { get; set; }

		public bool Equals(SpanInfo other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return Type == other.Type && SubType == other.SubType;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			return Equals((SpanInfo)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (SubType != null ? SubType.GetHashCode() : 0);
			}
		}

		public static bool operator ==(SpanInfo left, SpanInfo right) => Equals(left, right);

		public static bool operator !=(SpanInfo left, SpanInfo right) => !Equals(left, right);
	}
}
