using System.Collections.Generic;

namespace Elastic.Apm.Libraries.Newtonsoft.Json.Linq.JsonPath
{
	internal class ArrayMultipleIndexFilter : PathFilter
	{
		public ArrayMultipleIndexFilter(List<int> indexes) => Indexes = indexes;

		internal List<int> Indexes;

		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings)
		{
			foreach (var t in current)
			{
				foreach (var i in Indexes)
				{
					var v = GetTokenIndex(t, settings, i);

					if (v != null) yield return v;
				}
			}
		}
	}
}
