using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MetadataDto: IDto
	{
		public Service Service { get; set; }
		public Api.System System { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(MetadataDto)) { { "Service", Service }, { "System", System }, }.ToString();

		public void AssertValid()
		{
			Service.AssertValid();
			System?.AssertValid();
		}
	}
}
