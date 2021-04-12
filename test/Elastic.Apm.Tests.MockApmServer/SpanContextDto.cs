// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Model;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	internal class SpanContextDto : IDto
	{
		public Database Db { get; set; }

		public Destination Destination { get; set; }

		public Http Http { get; set; }

		[JsonProperty("tags")]
		public LabelsDictionary Labels { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanContextDto))
		{
			{ nameof(Db), Db }, { nameof(Http), Http }, { nameof(Labels), Labels }, { nameof(Destination), Destination }
		}.ToString();

		public void AssertValid()
		{
			Db?.AssertValid();
			Http?.AssertValid();
			Labels?.LabelsAssertValid();
			Destination?.AssertValid();
		}
	}
}
