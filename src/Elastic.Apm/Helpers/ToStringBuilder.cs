using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Apm.Helpers
{
	//
	// We need to implement IEnumerable to support collection initializer syntax
	//
	public struct ToStringBuilder : IEnumerable
	{
		private const int _stringBuilderInitialCapacity = 100;
		private readonly StringBuilder _stringBuilder;
		private bool _addedAny;

		public ToStringBuilder(string className)
		{
			_stringBuilder = new StringBuilder(_stringBuilderInitialCapacity);
			_stringBuilder.Append(className).Append("{");
			_addedAny = false;
		}

		public void Add(string propertyName, object propertyValue)
		{
			if (_addedAny) _stringBuilder.Append(", ");
			_stringBuilder.Append(propertyName).Append(": ").Append(propertyValue);
			_addedAny = true;
		}

		IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

		public override string ToString() => _stringBuilder + "}";
	}
}
