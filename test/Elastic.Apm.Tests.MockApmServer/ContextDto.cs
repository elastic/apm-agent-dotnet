using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class ContextDto: IDto
	{
		public Request Request { get; set; }
		public Response Response { get; set; }
		public Dictionary<string, string> Labels { get; set; }
		public User User { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(ContextDto))
		{
			{ "Request", Request },
			{ "Response", Response },
			{ "User", User },
			{ "Labels", Labels },
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
