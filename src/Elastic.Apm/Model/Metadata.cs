// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model
{
	[Specification("metadata.json")]
	internal class Metadata
	{
		/// <inheritdoc cref="Api.Cloud"/>
		public Api.Cloud Cloud { get; set; }

		public LabelsDictionary Labels { get; set; } = new LabelsDictionary();

		// ReSharper disable once UnusedAutoPropertyAccessor.Global - used by Json.Net
		public Service Service { get; set; }

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public Api.System System { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="Labels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => Labels.Count > 0;
	}
}
