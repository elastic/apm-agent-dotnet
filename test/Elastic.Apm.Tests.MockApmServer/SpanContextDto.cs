using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	internal class SpanContextDto: IDto
	{
		public Database Db { get; set; }
		public Http Http { get; set; }
		public Dictionary<string, string> Tags { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanContextDto))
		{
			{ nameof(Db), Db },
			{ nameof(Http), Http },
			{ nameof(Tags), Tags },
		}.ToString();

		public void AssertValid()
		{
			Db?.AssertValid();
			Http?.AssertValid();
			Tags?.TagsAssertValid();
		}
	}
}
