using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class ContextDto: IDto
	{
		public Request Request { get; set; }

		public Response Response { get; set; }

		[JsonProperty("tags")]
		public Dictionary<string, string> Labels { get; set; }

		public User User { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(ContextDto))
		{
			{ nameof(Request), Request },
			{ nameof(Response), Response },
			{ nameof(User), User },
			{ nameof(Labels), Labels },
		}.ToString();

		public void AssertValid()
		{
			Response?.AssertValid();
			Request?.AssertValid();
			Labels?.LabelsAssertValid();
			User?.AssertValid();
		}
	}
}
