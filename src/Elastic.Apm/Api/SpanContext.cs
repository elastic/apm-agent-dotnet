// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class SpanContext
	{
		private readonly Lazy<Dictionary<string, Label>> _labels = new Lazy<Dictionary<string, Label>>();

		public Database Db { get; set; }
		public Destination Destination { get; set; }
		public Http Http { get; set; }

		/// <summary>
		/// <seealso cref="ShouldSerializeLabels" />
		/// </summary>
		[JsonProperty("tags")]
		[JsonConverter(typeof(LabelsJsonConverter))]
		public Dictionary<string, Label> Labels => _labels.Value;

		/// <summary>
		/// Method to conditionally serialize <see cref="Labels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => _labels.IsValueCreated && Labels.Count > 0;

		public override string ToString() => new ToStringBuilder(nameof(SpanContext))
		{
			{ nameof(Db), Db }, { nameof(Http), Http }, { nameof(Labels), _labels }, { nameof(Destination), Destination }
		}.ToString();
	}
}
