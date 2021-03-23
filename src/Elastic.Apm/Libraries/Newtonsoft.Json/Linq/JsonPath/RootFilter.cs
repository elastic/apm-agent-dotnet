using System.Collections.Generic;

#nullable enable
namespace Elastic.Apm.Libraries.Newtonsoft.Json.Linq.JsonPath
{
	internal class RootFilter : PathFilter
	{
		public static readonly RootFilter Instance = new();

		private RootFilter() { }

		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings) => new[] { root };
	}
}
