// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Context
	{
		private Lazy<Dictionary<string, string>> _custom = new Lazy<Dictionary<string, string>>();
		private Lazy<Dictionary<string, Label>> _labels = new Lazy<Dictionary<string, Label>>();

		[JsonConverter(typeof(CustomJsonConverter))]
		public Dictionary<string, string> Custom => _custom.Value;

		/// <summary>
		/// <seealso cref="ShouldSerializeLabels" />
		/// </summary>
		[JsonProperty("tags")]
		[JsonConverter(typeof(LabelsJsonConverter))]
		public Dictionary<string, Label> Labels => _labels.Value;

		/// <summary>
		/// If a log record was generated as a result of a http request, the http interface can be used to collect this
		/// information.
		/// This property is by default null! You have to assign a <see cref="Request" /> instance to this property in order to use
		/// it.
		/// </summary>
		public Request Request { get; set; }

		/// <summary>
		/// If a log record was generated as a result of a http request, the http interface can be used to collect this
		/// information.
		/// This property is by default null! You have to assign a <see cref="Response" /> instance to this property in order to
		/// use
		/// it.
		/// </summary>
		public Response Response { get; set; }

		public User User { get; set; }

		internal Context DeepCopy()
		{
			var newItem = (Context)MemberwiseClone();

			newItem._custom = new Lazy<Dictionary<string, string>>();
			if (_custom.IsValueCreated)
				foreach (var item in _custom.Value)
					newItem._custom.Value[item.Key] = item.Value;

			newItem._labels = new Lazy<Dictionary<string, Label>>(() => new Dictionary<string, Label>());
			if (_labels.IsValueCreated)
				foreach (var item in _labels.Value)
					newItem._labels.Value[item.Key] = item.Value;

			newItem.Request = Request?.DeepCopy();
			newItem.Response?.DeepCopy();

			return newItem;
		}

		/// <summary>
		/// Method to conditionally serialize <see cref="Labels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => _labels.IsValueCreated && Labels.Count > 0;

		public bool ShouldSerializeCustom() => _custom.IsValueCreated && Custom.Count > 0;
	}
}
