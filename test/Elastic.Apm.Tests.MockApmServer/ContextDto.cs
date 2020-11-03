// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class ContextDto : IDto
	{
		[JsonProperty("tags")]
		public LabelsDictionary Labels { get; set; }

		public Request Request { get; set; }

		public Response Response { get; set; }

		public User User { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(ContextDto))
		{
			{ nameof(Request), Request }, { nameof(Response), Response }, { nameof(User), User }, { nameof(Labels), Labels }
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
