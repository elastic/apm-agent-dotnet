using System;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
// ReSharper disable MemberCanBePrivate.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MetadataDto
	{
		public Service Service { get; set; }
		public Api.System System { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(MetadataDto))
		{
			{ "Service", Service },
			{ "System", System },
		}.ToString();
	}
}
