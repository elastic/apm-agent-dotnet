// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using NJsonSchema;

namespace Elastic.Apm.Specification
{
	public static class JsonSchemaExtensions
	{
		public static string GetNameOrSpecificationId(this JsonSchema schema)
		{
			if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("$id", out var id))
				return id.ToString();

			if (schema is JsonSchemaProperty schemaProperty && !string.IsNullOrEmpty(schemaProperty.Name))
				return schemaProperty.Name;

			if (!string.IsNullOrEmpty(schema.DocumentPath))
				return Path.GetFileNameWithoutExtension(schema.DocumentPath);

			return null;
		}
	}
}
