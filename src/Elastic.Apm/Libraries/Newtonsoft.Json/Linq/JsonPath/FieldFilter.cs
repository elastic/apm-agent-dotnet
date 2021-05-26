using System.Collections.Generic;
using System.Globalization;
using Elastic.Apm.Libraries.Newtonsoft.Json.Utilities;

#nullable enable
namespace Elastic.Apm.Libraries.Newtonsoft.Json.Linq.JsonPath
{
	internal class FieldFilter : PathFilter
	{
		public FieldFilter(string? name) => Name = name;

		internal string? Name;

		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings)
		{
			foreach (var t in current)
			{
				if (t is JObject o)
				{
					if (Name != null)
					{
						var v = o[Name];

						if (v != null)
							yield return v;
						else if (settings?.ErrorWhenNoMatch ?? false)
							throw new JsonException("Property '{0}' does not exist on JObject.".FormatWith(CultureInfo.InvariantCulture, Name));
					}
					else
					{
						foreach (var p in o) yield return p.Value!;
					}
				}
				else
				{
					if (settings?.ErrorWhenNoMatch ?? false)
						throw new JsonException(
							"Property '{0}' not valid on {1}.".FormatWith(CultureInfo.InvariantCulture, Name ?? "*", t.GetType().Name));
				}
			}
		}
	}
}
