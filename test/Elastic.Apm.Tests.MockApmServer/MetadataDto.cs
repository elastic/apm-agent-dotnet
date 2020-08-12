// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MetadataDto : IDto
	{
		public Service Service { get; set; }
		public Api.System System { get; set; }
		public Dictionary<string, string> Labels { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(MetadataDto))
			{
				{ nameof(Service), Service },
				{ nameof(System), System },
				{ nameof(Labels), AbstractConfigurationReader.ToLogString(Labels) }
			}.ToString();

		public void AssertValid()
		{
			Service.AssertValid();
			System?.AssertValid();
		}
	}
}
