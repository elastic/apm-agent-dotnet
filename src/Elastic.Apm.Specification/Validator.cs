using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Report.Serialization;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json.Serialization;
using NJsonSchema;

namespace Elastic.Apm.Specification
{
	public class ValidationOptions
	{
		public bool CheckNullable { get; set; }
	}

	/// <summary>
	/// Validates types against the APM server specification
	/// </summary>
	/// <remarks>
	///	Not thread-safe when more than one instance points to the same directory because
	/// directories and files are deleted and created as needed.
	/// </remarks>
	public class Validator
	{
		private static readonly HashSet<string> NumericTypeNames = new HashSet<string>
		{
			"System.Int32",
			"System.UInt16",
			"System.Int16",
			"System.Byte",
			"System.SByte",
			"System.Int64",
			"System.UInt32",
			"System.Single",
			"System.Decimal",
			"System.Double",
			"System.UInt64",
		};

		private static readonly Regex SpecPathRegex =
			new Regex("^.*?/(?<path>docs/spec/.*?\\.json)$", RegexOptions.ExplicitCapture);

		private readonly string _directory;
		public string Branch { get; private set; }

		public Validator(string branch, string directory)
		{
			if (string.IsNullOrWhiteSpace(branch))
				throw new ArgumentException("must have a value", nameof(branch));

			if (string.IsNullOrWhiteSpace(directory))
				throw new ArgumentException("must have a value", nameof(directory));

			try
			{
				_directory = Path.GetFullPath(directory);
			}
			catch (Exception e)
			{
				throw new ArgumentException("must be a valid path", nameof(directory), e);
			}

			Branch = branch;
		}

		public async Task DownloadAsync(string branch, bool overwrite)
		{
			var branchDirectory = Path.Combine(_directory, branch);

			// assume that if the branch already exists, it contains the specs
			if (Directory.Exists(branchDirectory))
			{
				if (!overwrite)
					return;

				try
				{
					Directory.Delete(branchDirectory, true);
				}
				catch (Exception e)
				{
					throw new Exception($"Exception deleting directory '{branchDirectory}', {e.Message}", e);
				}
			}

			using var client = new HttpClient();
			client.DefaultRequestHeaders.UserAgent.Clear();
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("apm-agent-dotnet", "1"));

			// use the GitHub api to download the tar.gz and extract the specs from the stream.
			// This is considerably faster than downloading many small files.
			var response = await client.GetAsync($"https://api.github.com/repos/elastic/apm-server/tarball/{branch}")
				.ConfigureAwait(false);

			using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
			using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
			using var tarStream = new TarInputStream(gzipStream, Encoding.UTF8);

			while (true)
			{
				var entry = tarStream.GetNextEntry();
				if (entry == null) break;

				if (entry.TarHeader.TypeFlag == TarHeader.LF_LINK || entry.TarHeader.TypeFlag == TarHeader.LF_SYMLINK)
					continue;

				var name = entry.Name;
				var match = SpecPathRegex.Match(name);

				// only interested in the spec files
				if (!match.Success)
					continue;

				name = match.Groups["path"].Value;
				name = name.Replace('/', Path.DirectorySeparatorChar);

				var destFile = Path.Combine(branchDirectory, name);
				var parentDirectory = Path.GetDirectoryName(destFile);

				try
				{
					Directory.CreateDirectory(parentDirectory);
				}
				catch (Exception e)
				{
					throw new Exception($"Exception creating directory '{parentDirectory}', {e.Message}", e);
				}

				using var fileStream = File.Create(destFile);
				tarStream.CopyEntryContents(fileStream);
			}
		}

		/// <summary>
		/// Loads the schema for the given specification id. Downloads the schema if it not already downloaded.
		/// </summary>
		/// <param name="specificationId"></param>
		/// <returns>
		/// The schema for the specification id.
		/// </returns>
		/// <exception cref="FileNotFoundException">
		/// The given specification id does not exist
		/// </exception>
		public async Task<JsonSchema> LoadSchemaAsync(string specificationId)
		{
			var branchDirectory = Path.Combine(_directory, Branch);
			await DownloadAsync(branchDirectory, false).ConfigureAwait(false);

			var path = Path.Combine(branchDirectory, specificationId);

			if (!File.Exists(path))
				throw new FileNotFoundException($"'{specificationId}' does not exist at {path}", path);

			return await JsonSchema.FromFileAsync(path).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the specification id for the given type.
		/// </summary>
		/// <param name="type">the type to get the specification id for</param>
		/// <returns>the specification id</returns>
		/// <exception cref="Exception">
		/// A type does not implement a spec or implements more than one spec
		/// </exception>
		public string GetSpecificationIdForType(Type type)
		{
			var specAttribute = type.GetCustomAttribute<SpecificationAttribute>();
			var specAttributes = type.GetInterfaces()
				.Select(i => i.GetCustomAttribute<SpecificationAttribute>())
				.Where(a => a != null)
				.ToList();

			if (specAttribute != null)
				specAttributes.Add(specAttribute);

			if (!specAttributes.Any())
				throw new Exception($"{type.FullName} has does not have a spec");

			if (specAttributes.Count > 1)
			{
				var paths = specAttributes.Select(s => s.Path);
				throw new Exception($"{type.FullName} has more than one spec {string.Join(", ", paths)}");
			}

			return specAttributes.Single().Path;
		}

		/// <summary>
		/// Validates the specification against the type. A specification may be more lenient, or define more
		/// properties than are specified on a type.
		/// </summary>
		/// <remarks>
		/// It's expected that the type is the implementation of the entire specification and not a subset
		/// of certain optional properties.
		/// </remarks>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="Exception">
		/// A type does not implement a spec or implements more than one spec
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// Spec file does not exist
		/// </exception>
		public async Task<ValidationResult> ValidateSpecAgainstTypeAsync(Type type)
		{
			var specificationId = GetSpecificationIdForType(type);
			return await ValidateSpecAgainstTypeAsync(type, specificationId);
		}

		private async Task<ValidationResult> ValidateSpecAgainstTypeAsync(Type type, string specificationId)
		{
			await DownloadAsync(Branch, false);
			var schema = await LoadSchemaAsync(specificationId).ConfigureAwait(false);
			var result = new ValidationResult(type, specificationId);

			ValidateSpecProperties(type, schema, result);

			foreach (var inheritedSchema in schema.AllInheritedSchemas)
				ValidateSpecProperties(type, inheritedSchema, result);

			return result;
		}

		/// <summary>
		/// Get the properties for the given specification type, using the APM agent's serialization components.
		/// </summary>
		/// <remarks>
		/// Encapsulates the APM agent implementation for how specification constraints are enforced. This currently
		/// uses Json.NET for serialized property names and constraining max length.
		/// </remarks>
		/// <param name="specType">the specification type</param>
		/// <returns></returns>
		private static SpecificationProperty[] GetProperties(Type specType)
		{
			var resolver = new ElasticApmContractResolver(new EnvironmentConfigurationReader());
			JsonObjectContract contract;

			try
			{
				// the json schema may indicate a type is an "object", but the agent may model it in some other way
				// e.g. samples on metricset is modelled as a collection. In these scenarios, we won't be dealing with
				// an object contract and won't be able to statically determine validity of the type to the schema through reflection.
				// The only way to validate these against the schema is to serialize the types.
				contract = (JsonObjectContract)resolver.ResolveContract(specType);
			}
			catch (InvalidCastException e)
			{
				throw new ContractResolveException(e.Message);
			}

			var specProperties = new List<SpecificationProperty>(contract.Properties.Count);
			foreach (var jsonProperty in contract.Properties)
			{
				if (jsonProperty.Ignored)
					continue;

				if (jsonProperty != null)
				{
					var specProperty = new SpecificationProperty(jsonProperty.PropertyName, jsonProperty.PropertyType, specType);

					if (jsonProperty.Converter != null && jsonProperty.Converter is TrimmedStringJsonConverter trimmedStringJsonConverter)
						specProperty.MaxLength = trimmedStringJsonConverter.MaxLength;

					specProperties.Add(specProperty);
				}
			}

			return specProperties.ToArray();
		}

		private static void ValidateSpecProperties(Type specType, JsonSchema schema, ValidationResult result)
		{
			SpecificationProperty[] properties;

			try
			{
				properties = GetProperties(specType);
			}
			catch (ContractResolveException e)
			{
				result.AddIgnore(new ValidationIgnore(schema.GetNameOrSpecificationId(), specType.Name, e.Message));
				return;
			}

			ValidateSpecProperties(specType, schema, properties, result);

			foreach (var inheritedSchema in schema.AllInheritedSchemas)
				ValidateSpecProperties(specType, inheritedSchema, properties, result);

			if (schema.AnyOf != null && schema.AnyOf.Count > 0)
			{
				var anyOfResults = Enumerable.Repeat(new ValidationResult(specType, result.SpecificationId), schema.AnyOf.Count).ToList();
				var index = 0;
				foreach (var anyOfSchema in schema.AnyOf)
				{
					ValidateSpecProperties(specType, anyOfSchema, properties, anyOfResults[index]);
					++index;
				}

				// at least one must be successful
				if (!anyOfResults.Any(r => r.Success))
				{
					var errors = anyOfResults.Select(r => r.ToString());
					result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), specType.Name, $"anyOf failure: {string.Join(",", errors)}"));
				}
			}

			if (schema.OneOf != null && schema.OneOf.Count > 0)
			{
				var oneOfResults = Enumerable.Repeat(new ValidationResult(specType, result.SpecificationId), schema.AnyOf.Count).ToList();
				var index = 0;
				foreach (var oneOfSchema in schema.OneOf)
				{
					ValidateSpecProperties(specType, oneOfSchema, properties, oneOfResults[index]);
					++index;
				}

				// only one must be successful
				if (!oneOfResults.Any(r => r.Success))
				{
					var errors = oneOfResults.Select(r => r.ToString());
					result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), specType.Name, $"oneOf all failure: {string.Join(",", errors)}"));
				}
				else if (oneOfResults.Count(r => r.Success) > 0)
				{
					var errors = oneOfResults.Where(r => r.Success).Select(r => r.ToString());
					result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), specType.Name, $"oneOf more than one successful failure: {string.Join(",", errors)}"));
				}
			}

		}

		private static void ValidateSpecProperties(Type specType, JsonSchema schema, SpecificationProperty[] properties, ValidationResult result)
		{
			foreach (var kv in schema.ActualProperties)
			{
				var schemaProperty = kv.Value.ActualSchema;
				var name = kv.Value.Name;
				var specTypeProperty = properties.SingleOrDefault(p => p.Name == name);
				if (specTypeProperty == null)
				{
					if (!schemaProperty.Type.HasFlag(JsonObjectType.Null))
						result.AddError(ValidationError.NotFound(specType, schema.GetNameOrSpecificationId(), name));

					continue;
				}

				// check certain .NET types first before defaulting to the JSON schema flags.
				// A property might be represented as more than one "primitive" type e.g. ["integer", "string", "null],
				// so it's better to look at the .NET type first and try to look for the associated JSON schema flag.
				// We could also look to see if more than one JSON schema flag is set (removing null flag first).
				switch (specTypeProperty.PropertyType.FullName)
				{
					case "System.UInt16":
					case "System.Int16":
					case "System.Byte":
					case "System.SByte":
					case "System.UInt32":
					case "System.Single":
					case "System.Decimal":
					case "System.Double":
					case "System.UInt64":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Number))
							CheckNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), name,
								$"expecting 'number' type but found {schemaProperty.Type}"));
						break;
					case "System.Int64":
					case "System.Int32":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Number))
							CheckNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Integer))
							CheckInteger(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), name,
								$"expecting 'number' or 'integer' type but found {schemaProperty.Type}"));
						break;
					case "System.String":
						if (schemaProperty.Type.HasFlag(JsonObjectType.String))
							CheckString(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), name,
								$"expecting 'string' type but found {schemaProperty.Type}"));
						break;
					case "System.Boolean":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Boolean))
							CheckBoolean(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), name,
								$"expecting 'boolean' type but found {schemaProperty.Type}"));
						break;
					default:
						var typeFlags = schemaProperty.Type;

						// remove null flag if it exists, and check to see if we have more than one flag
						if (typeFlags.HasFlag(JsonObjectType.Null))
							typeFlags &= ~JsonObjectType.Null;

						// is there still more than one flag? Check to see if it's a power of two. If so, based on the .NET type not being a
						// primitive type, we would expect the presence of the schema "object" or "array" type in the majority of cases, so check
						// these first. For types with custom serialization, the schema type may be a primitive type.
						if ((typeFlags & (typeFlags - 1)) != 0)
						{
							if (typeFlags.HasFlag(JsonObjectType.Object))
								ValidateSpecProperties(specTypeProperty.PropertyType, schemaProperty, result);
							else if (typeFlags.HasFlag(JsonObjectType.Array))
							{
								if (IsEnumerableType(specTypeProperty.PropertyType, out var elementType))
									ValidateSpecProperties(elementType, schemaProperty.Item.ActualSchema, result);
							}
							else
							{
								result.AddIgnore(new ValidationIgnore(schema.GetNameOrSpecificationId(), name,
									$"Cannot statically check types. .NET type id {specType} and schema type is {schemaProperty.Type}"));
							}
						}
						else if (typeFlags.HasFlag(JsonObjectType.Boolean))
							CheckBoolean(specType, schema, schemaProperty, specTypeProperty, result);
						else if (typeFlags.HasFlag(JsonObjectType.Integer))
							CheckInteger(specType, schema, schemaProperty, specTypeProperty, result);
						else if (typeFlags.HasFlag(JsonObjectType.Number))
							CheckNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else if (typeFlags.HasFlag(JsonObjectType.String))
							CheckString(specType, schema, schemaProperty, specTypeProperty, result);
						else if (typeFlags.HasFlag(JsonObjectType.Object))
							ValidateSpecProperties(specTypeProperty.PropertyType, schemaProperty, result);
						else if (typeFlags.HasFlag(JsonObjectType.Array))
						{
							if (IsEnumerableType(specTypeProperty.PropertyType, out var elementType))
								ValidateSpecProperties(elementType, schemaProperty.Item.ActualSchema, result);
						}
						break;
				}
			}
		}

		private static void CheckString(Type specType, JsonSchema schema, JsonSchema property, SpecificationProperty specTypeProperty,
			ValidationResult result
		)
		{
			CheckType(specType, "string", schema, property, specTypeProperty, result, t => t == typeof(string));
			CheckMaxLength(specType, schema, property, specTypeProperty, result);
		}

		private static void CheckBoolean(Type specType, JsonSchema schema, JsonSchema property, SpecificationProperty specTypeProperty,
			ValidationResult result
		) =>
			CheckType(specType, "boolean", schema, property, specTypeProperty, result, t => t == typeof(bool));

		private static void CheckInteger(Type specType, JsonSchema schema, JsonSchema property, SpecificationProperty specTypeProperty,
			ValidationResult result
		) =>
			CheckType(specType, "integer", schema, property, specTypeProperty, result, t => t == typeof(int) || t == typeof(long));

		private static void CheckNumber(Type specType, JsonSchema schema, JsonSchema property, SpecificationProperty specTypeProperty,
			ValidationResult result
		) =>
			CheckType(specType, "number", schema, property, specTypeProperty, result, t => NumericTypeNames.Contains(t.FullName));

		private static void CheckType(Type specType, string expectedType, JsonSchema schema, JsonSchema property, SpecificationProperty specificationProperty,
			ValidationResult result, Func<Type, bool> typeCheck
		)
		{
			var propertyType = specificationProperty.PropertyType;
			var nullable = !propertyType.IsValueType;

			if (IsNullableType(propertyType))
			{
				nullable = true;
				propertyType = Nullable.GetUnderlyingType(specificationProperty.PropertyType);
			}

			if (!typeCheck(propertyType))
				result.AddError(ValidationError.ExpectedType(specType, expectedType, schema.GetNameOrSpecificationId(), specificationProperty.Name, propertyType.FullName));

			// TODO: don't check for null for now...
			// if (property.Type.HasFlag(JsonObjectType.Null) && !nullable)
			// 	result.AddError(new ValidationError(specType, schema.GetSpecificationIdOrName(), property.Name, "expected type to be nullable"));
		}

		private static void CheckMaxLength(Type specType, JsonSchema schema, JsonSchema schemaProperty, SpecificationProperty specificationProperty,
			ValidationResult result
		)
		{
			if (schemaProperty.MaxLength.HasValue)
			{
				var maxLength = schemaProperty.MaxLength.Value;
				if (!specificationProperty.MaxLength.HasValue)
					result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), specificationProperty.Name, $"expected property to enforce maxLength of {maxLength} but does not"));
				else
				{
					if (specificationProperty.MaxLength != maxLength)
						result.AddError(new ValidationError(specType, schema.GetNameOrSpecificationId(), specificationProperty.Name,
							$"property max length {specificationProperty.MaxLength} not equal to spec maxLength {maxLength}"));
				}
			}
		}

		private static bool IsNullableType(Type type) =>
			type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

		private static bool IsEnumerableType(Type type, out Type elementType)
		{
			elementType = default;
			foreach (var @interface in type.GetInterfaces())
			{
				if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					elementType = @interface.GetGenericArguments()[0];
					return true;
				}
			}

			return false;
		}
	}

	internal class ContractResolveException : Exception
	{
		public ContractResolveException(string message) : base(message)
		{
		}
	}
}
