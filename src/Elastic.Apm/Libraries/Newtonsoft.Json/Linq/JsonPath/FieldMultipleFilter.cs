using System.Collections.Generic;
using System.Globalization;
using Elastic.Apm.Libraries.Newtonsoft.Json.Utilities;
#if !HAVE_LINQ
using Elastic.Apm.Libraries.Newtonsoft.Json.Utilities.LinqBridge;
#else
using System.Linq;
#endif

namespace Elastic.Apm.Libraries.Newtonsoft.Json.Linq.JsonPath
{
	internal class FieldMultipleFilter : PathFilter
	{
		public FieldMultipleFilter(List<string> names) => Names = names;

		internal List<string> Names;

		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings)
		{
			foreach (var t in current)
			{
				if (t is JObject o)
				{
					foreach (var name in Names)
					{
						var v = o[name];

						if (v != null) yield return v;

						if (settings?.ErrorWhenNoMatch ?? false)
							throw new JsonException("Property '{0}' does not exist on JObject.".FormatWith(CultureInfo.InvariantCulture, name));
					}
				}
				else
				{
					if (settings?.ErrorWhenNoMatch ?? false)
					{
						throw new JsonException("Properties {0} not valid on {1}.".FormatWith(CultureInfo.InvariantCulture, string.Join(", ",
							Names.Select(n => "'" + n + "'")
#if !HAVE_STRING_JOIN_WITH_ENUMERABLE
								.ToArray()
#endif
						), t.GetType().Name));
					}
				}
			}
		}
	}
}
