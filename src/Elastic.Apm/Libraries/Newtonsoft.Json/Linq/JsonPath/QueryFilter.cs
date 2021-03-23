using System.Collections.Generic;

namespace Elastic.Apm.Libraries.Newtonsoft.Json.Linq.JsonPath
{
	internal class QueryFilter : PathFilter
	{
		public QueryFilter(QueryExpression expression) => Expression = expression;

		internal QueryExpression Expression;

		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings)
		{
			foreach (var t in current)
			{
				foreach (var v in t)
				{
					if (Expression.IsMatch(root, v, settings)) yield return v;
				}
			}
		}
	}
}
