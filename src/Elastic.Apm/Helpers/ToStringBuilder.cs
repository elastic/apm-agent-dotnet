using System;
using System.Collections;
using System.Text;

namespace Elastic.Apm.Helpers
{
	//
	// We need to implement IEnumerable to support collection initializer syntax
	//
	public struct ToStringBuilder : IEnumerable
	{
		private const int StringBuilderInitialCapacity = 100;
		private readonly StringBuilder _stringBuilder;
		private bool _addedAny;

		public ToStringBuilder(string className)
		{
			_stringBuilder = new StringBuilder(StringBuilderInitialCapacity);
			_stringBuilder.Append(className).Append("{");
			_addedAny = false;
		}

		public void Add(string propertyName, object propertyValue)
		{
			if (_addedAny) _stringBuilder.Append(", ");
			_stringBuilder.Append(propertyName).Append(": ");
			if (propertyValue == null)
				_stringBuilder.Append("null");
			else
				_stringBuilder.Append(propertyValue);
			_addedAny = true;
		}

		IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

		public override string ToString() => _stringBuilder + "}";
	}
}
