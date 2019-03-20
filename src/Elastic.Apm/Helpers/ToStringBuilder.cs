using System.Text;

namespace Elastic.Apm.Helpers
{
	public class ToStringBuilder
	{
		private readonly StringBuilder _stringBuilder = new StringBuilder();

		public ToStringBuilder(string className)
		{
			_stringBuilder.Append(className);
			_stringBuilder.Append("{");
		}

		private bool _addedAny;

		public object this[string propertyName]
		{
			set => Add(propertyName, value);
		}

		public ToStringBuilder Add(string propertyName, object propertyValue)
		{
			if (_addedAny) _stringBuilder.Append(", ");

			_stringBuilder.Append(propertyName).Append(": ").Append(propertyValue);

			_addedAny = true;
			return this;
		}

		public override string ToString() => _stringBuilder + "}";
	}
}
