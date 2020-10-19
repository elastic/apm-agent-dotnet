// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;

namespace Elastic.Apm.Specification
{
	public class ValidationIgnore
	{
		public string Schema { get; }
		public string PropertyName { get; }
		public string Message { get; }

		public ValidationIgnore(string schema, string propertyName, string message)
		{
			Schema = Path.GetFileNameWithoutExtension(schema);
			PropertyName = propertyName;
			Message = message;
		}
	}

	public class ValidationError
	{
		public string Schema { get; }
		public string PropertyName { get; }
		public string Message { get; }

		public Type SpecType { get; }

		public ValidationError(Type specType, string schema, string propertyName, string message)
		{
			SpecType = specType;
			Schema = Path.GetFileNameWithoutExtension(schema);
			PropertyName = propertyName;
			Message = message;
		}

		public static ValidationError NotFound(Type specType, string id, string name) =>
			new ValidationError(specType, id, name, "not found");

		public static ValidationError ExpectedType(Type specType, Type propertyType, string expectedType, string id, string name) =>
			new ValidationError(specType, id, name, $"expected type '{expectedType}' but found '{propertyType.FullName}'");

		public override string ToString() => $"{SpecType}, property name: {PropertyName}, schema: {Schema}. {Message}.";
	}
}
