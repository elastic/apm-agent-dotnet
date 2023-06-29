// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Api.Kubernetes;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class System
	{
		public KubernetesMetadata Kubernetes { get; set; }

		public Container Container { get; set; }

		/// <summary>
		/// Hostname detected by the APM agent. It usually contains what the hostname command returns on the host machine.
		/// It will be used as the event's hostname if <see cref="ConfiguredHostName"/> is not present.
		/// </summary>
		[MaxLength]
		[JsonProperty("detected_hostname")]
		public string DetectedHostName { get; set; }

		/// <summary>
		/// Configured name of the host the monitored service is running on. It should only be sent when configured by the user.
		/// If given, it is used as the event's hostname.
		/// </summary>
		[MaxLength]
		[JsonProperty("configured_hostname")]
		public string ConfiguredHostName { get; set; }

		/// <summary>
		/// The hostname configured by the user, if configured, otherwise the detected hostname.
		/// </summary>
		[MaxLength]
		[JsonProperty("hostname")]
		[Obsolete("Deprecated. Use " + nameof(ConfiguredHostName))]
		public string HostName
		{
			get => ConfiguredHostName ?? DetectedHostName;
			set => ConfiguredHostName = value;
		}

		public override string ToString() =>
			new ToStringBuilder(nameof(System))
			{
				{ nameof(Container), Container },
				{ nameof(ConfiguredHostName), ConfiguredHostName },
				{ nameof(DetectedHostName), DetectedHostName },
#pragma warning disable 618
				{ nameof(HostName), HostName }
#pragma warning restore 618
			}.ToString();
	}
}
