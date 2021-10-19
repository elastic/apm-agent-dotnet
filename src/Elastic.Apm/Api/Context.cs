// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Context
	{
		private Lazy<Dictionary<string, string>> _custom = new Lazy<Dictionary<string, string>>();

		[JsonConverter(typeof(CustomJsonConverter))]
		public Dictionary<string, string> Custom => _custom.Value;

		internal Lazy<LabelsDictionary> InternalLabels = new Lazy<LabelsDictionary>();

		/// <summary>
		/// <seealso cref="ShouldSerializeLabels" />
		/// </summary>
		[Obsolete(
			"Instead of this dictionary, use the `SetLabel` method which supports more types than just string. This property will be removed in a future release.")]
		[JsonProperty("tags")]
		public Dictionary<string, string> Labels => InternalLabels.Value;

		/// <summary>
		/// Holds details related to message receiving and publishing if the captured event integrates with a messaging system
		/// </summary>
		public Message Message { get; set; }

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

		/// <summary>
		/// Service related information can be sent per event. Provided information will override the more generic information from
		/// metadata, non provided fields will be set according to the metadata information.
		/// </summary>
		[JsonProperty("service")]
		internal Service Service { get; set; }

		public User User { get; set; }

		internal Context DeepCopy()
		{
			var newItem = (Context)MemberwiseClone();

			newItem._custom = new Lazy<Dictionary<string, string>>();
			if (_custom.IsValueCreated)
			{
				foreach (var item in _custom.Value)
					newItem._custom.Value[item.Key] = item.Value;
			}

			newItem.InternalLabels = new Lazy<LabelsDictionary>();
			if (InternalLabels.IsValueCreated)
			{
				foreach (var item in InternalLabels.Value.InnerDictionary)
					newItem.InternalLabels.Value.InnerDictionary[item.Key] = item.Value;

				foreach (var item in InternalLabels.Value)
					newItem.InternalLabels.Value[item.Key] = item.Value;
			}

			newItem.Request = Request?.DeepCopy();
			newItem.Response = Response?.DeepCopy();

			return newItem;
		}

		/// <summary>
		/// Method to conditionally serialize <see cref="InternalLabels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => InternalLabels.IsValueCreated && InternalLabels.Value.MergedDictionary.Count > 0;

		public bool ShouldSerializeCustom() => _custom.IsValueCreated && Custom.Count > 0;
	}
}
