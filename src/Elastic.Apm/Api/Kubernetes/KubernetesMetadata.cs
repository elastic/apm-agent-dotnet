// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api.Kubernetes
{
	public class KubernetesMetadata
	{
		[MaxLength]
		public string Namespace { get; set; }

		public Node Node { get; set; }

		public Pod Pod { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(KubernetesMetadata)) { { nameof(Namespace), Namespace }, { nameof(Node), Node }, { nameof(Pod), Pod } }
				.ToString();
	}
}
