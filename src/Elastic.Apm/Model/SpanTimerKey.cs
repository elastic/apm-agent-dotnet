// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Encapsulates type and subtype
	/// </summary>
	internal readonly record struct SpanTimerKey
	{
		public SpanTimerKey(string type, string subType) => (Type, SubType) = (type, subType);
		public SpanTimerKey(string type) => (Type, SubType) = (type, null);

		public readonly string Type { get; }
		public readonly string SubType { get; }

		public static SpanTimerKey AppSpanType => new("app");
	}
}
