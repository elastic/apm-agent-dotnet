// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;

namespace Elastic.Apm.Specification
{
	public class TypeValidationIgnore
	{
		public string Schema { get; }
		public string PropertyName { get; }
		public string Message { get; }

		public TypeValidationIgnore(string schema, string propertyName, string message)
		{
			Schema = schema;
			PropertyName = propertyName;
			Message = message;
		}

		public override string ToString() => $"ignored property name: {PropertyName}, schema: {Schema}. {Message}.";
	}

	public class TypeValidationError
	{
		public string Schema { get; }
		public string PropertyName { get; }
		public string Message { get; }

		public Type SpecType { get; }

		public TypeValidationError(Type specType, string schema, string propertyName, string message)
		{
			SpecType = specType;
			Schema = schema;
			PropertyName = propertyName;
			Message = message;
		}

		public static TypeValidationError NotFound(Type specType, string id, string name) =>
			new TypeValidationError(specType, id, name, "not found");

		public static TypeValidationError ExpectedType(Type specType, Type propertyType, string expectedType, string id, string name) =>
			new TypeValidationError(specType, id, name, $"expected type '{expectedType}' but found '{propertyType.FullName}'");

		public override string ToString() => $"{SpecType}, property name: {PropertyName}, schema: {Schema}. {Message}.";
	}
}
