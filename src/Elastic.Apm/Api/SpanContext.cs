// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Text;
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

		public SpanService Service { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanContext))
		{
			{ nameof(Db), Db }, { nameof(Http), Http }, { "Labels", InternalLabels }, { nameof(Destination), Destination },
		}.ToString();
	}

	public class SpanService
	{
		public Target Target { get; private set; }

		public SpanService(Target target) => Target = target;
	}

	public class Target
	{
		public string Type { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Indicates to only use <see cref="Name"/> in <see cref="ToDestinationServiceResource"/>.
		/// E.g. HTTP spans only use name in `Destination.Service.Resource`.
		/// </summary>
		private bool _onlyUseName;

		private Target() { }

		public Target(string type, string name) => (Type, Name) = (type, name);
		internal Target(string type, string name, bool onlyUseName = false) => (Type, Name, _onlyUseName) = (type, name, onlyUseName);
		public static Target TargetWithName(string name) => new Target { Name = name };
		public static Target TargetWithType(string type) => new Target { Type = type };

		public string ToDestinationServiceResource()
		{
			var sb = new StringBuilder();

			if (!_onlyUseName)
			{
				if (!string.IsNullOrEmpty(Type))
					sb.Append(Type);
				if (string.IsNullOrEmpty(Name)) return sb.ToString();

				if (sb.Length > 0)
					sb.Append("/");
			}
			sb.Append(Name);
			return sb.ToString();
		}
	}
}
