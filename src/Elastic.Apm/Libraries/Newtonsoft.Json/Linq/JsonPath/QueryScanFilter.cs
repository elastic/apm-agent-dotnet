using System.Collections.Generic;

#nullable enable
namespace Elastic.Apm.Libraries.Newtonsoft.Json.Linq.JsonPath
{
	internal class QueryScanFilter : PathFilter
	{
		public QueryScanFilter(QueryExpression expression) => Expression = expression;

		internal QueryExpression Expression;

		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings)
		{
			foreach (var t in current)
			{
				if (t is JContainer c)
				{
					foreach (var d in c.DescendantsAndSelf())
					{
						if (Expression.IsMatch(root, d, settings)) yield return d;
					}
				}
				else
				{
					if (Expression.IsMatch(root, t, settings)) yield return t;
				}
			}
		}
	}
}
