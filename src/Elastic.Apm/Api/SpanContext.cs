// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class SpanContext
	{
		internal readonly Lazy<LabelsDictionary> InternalLabels = new Lazy<LabelsDictionary>();

		public Database Db { get; set; }
		public Destination Destination { get; set; }
		public Http Http { get; set; }
		public Message Message { get; set; }

		/// <summary>
		/// <seealso cref="ShouldSerializeLabels" />
		/// </summary>
		[JsonProperty("tags")]
		[Obsolete(
			"Instead of this dictionary, use the `SetLabel` method which supports more types than just string. This property will be removed in a future release.")]
		public Dictionary<string, string> Labels => InternalLabels.Value;

		/// <summary>
		/// Method to conditionally serialize <see cref="InternalLabels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => InternalLabels.IsValueCreated && InternalLabels.Value.MergedDictionary.Count > 0;

		public override string ToString() => new ToStringBuilder(nameof(SpanContext))
		{
			{ nameof(Db), Db },
			{ nameof(Http), Http },
			{ "Labels", InternalLabels },
			{ nameof(Destination), Destination },
		}.ToString();
	}
}
