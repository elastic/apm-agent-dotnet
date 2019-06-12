using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	internal class SpanContextDto
	{
		public Database Db { get; set; }
		public Http Http { get; set; }
		public Dictionary<string, string> Tags { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanContextDto))
		{
			{ "Db", Db },
			{ "Http", Http },
			{ "Tags", Tags },
		}.ToString();
	}
}
