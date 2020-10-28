using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Elastic.Apm.Report.Serialization;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json.Serialization;
using NJsonSchema;

namespace Elastic.Apm.Specification
{
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
		public string Branch { get; }

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

		/// <summary>
		/// Downloads the APM server specifications for a given branch
		/// </summary>
		/// <param name="branch"></param>
		/// <param name="overwrite">If <c>true</c>, overwrite existing files.</param>
		/// <returns>A task that can be awaited</returns>
		/// <exception cref="Exception">
		/// Exception deleting current directory, or creating new directory.
		/// </exception>
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
#if !NETSTANDARD
			// force use of TLS 1.2 on older Full Framework, in order to call GitHub API
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

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

			try
			{
				return await JsonSchema.FromFileAsync(path).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				throw new JsonSchemaException($"Cannot load schema from path {path}, {e.Message}", e);
			}
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
		/// Validates an agent type with the APM server specification. The validation options determine
		/// how validation is performed.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="validation">The validation</param>
		/// <returns></returns>
		/// <exception cref="Exception">
		/// A type does not implement a spec or implements more than one spec
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// Spec file does not exist
		/// </exception>
		public async Task<TypeValidationResult> ValidateAsync(Type type, Validation validation)
		{
			var specificationId = GetSpecificationIdForType(type);
			return await ValidateAsync(type, specificationId, validation);
		}

		private async Task<TypeValidationResult> ValidateAsync(Type type, string specificationId, Validation validation)
		{
			await DownloadAsync(Branch, false);
			var schema = await LoadSchemaAsync(specificationId).ConfigureAwait(false);
			var result = new TypeValidationResult(type, specificationId, validation);

			ValidateProperties(type, schema, result);

			foreach (var inheritedSchema in schema.AllInheritedSchemas)
				ValidateProperties(type, inheritedSchema, result);

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
		private static ImplementationProperty[] GetProperties(Type specType)
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

			var specProperties = new List<ImplementationProperty>(contract.Properties.Count);
			foreach (var jsonProperty in contract.Properties)
			{
				if (jsonProperty.Ignored)
					continue;

				var implementationProperty = new ImplementationProperty(jsonProperty.PropertyName, jsonProperty.PropertyType, specType);
				var maxLength = (MaxLengthAttribute)(jsonProperty.AttributeProvider.GetAttributes(typeof(MaxLengthAttribute), true).FirstOrDefault());

				if (maxLength != null)
					implementationProperty.MaxLength = maxLength.Length;

				specProperties.Add(implementationProperty);
			}

			return specProperties.ToArray();
		}

		private static void ValidateProperties(Type specType, JsonSchema schema, TypeValidationResult result)
		{
			ImplementationProperty[] properties;

			try
			{
				properties = GetProperties(specType);
			}
			catch (ContractResolveException e)
			{
				result.AddIgnore(new TypeValidationIgnore(schema.GetNameOrSpecificationId(), specType.Name, e.Message));
				return;
			}

			ValidateProperties(specType, schema, properties, result);

			foreach (var inheritedSchema in schema.AllInheritedSchemas)
				ValidateProperties(specType, inheritedSchema, properties, result);

			if (schema.AnyOf.Count > 0)
			{
				var anyOfResults = Enumerable
					.Range(1, schema.AnyOf.Count)
					.Select(_ => new TypeValidationResult(specType, result.SpecificationId, result.Validation))
					.ToList();

				var index = 0;
				foreach (var anyOfSchema in schema.AnyOf)
				{
					ValidateProperties(specType, anyOfSchema, properties, anyOfResults[index]);
					++index;
				}

				// at least one must be successful
				if (!anyOfResults.Any(r => r.Success))
				{
					var errors = anyOfResults.Select(r => r.ToString());
					result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(), specType.Name,
						$"anyOf failure: {string.Join(",", errors)}"));
				}
			}

			if (schema.OneOf.Count > 0)
			{
				var oneOfResults = Enumerable
					.Range(1, schema.OneOf.Count)
					.Select(_ => new TypeValidationResult(specType, result.SpecificationId, result.Validation))
					.ToList();

				var index = 0;
				foreach (var oneOfSchema in schema.OneOf)
				{
					ValidateProperties(specType, oneOfSchema, properties, oneOfResults[index]);
					++index;
				}

				// only one must be successful
				if (!oneOfResults.Any(r => r.Success))
				{
					var errors = oneOfResults.Select(r => r.ToString());
					result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(), specType.Name,
						$"oneOf all failure: {string.Join(",", errors)}"));
				}
				else if (oneOfResults.Count(r => r.Success) > 0)
				{
					var errors = oneOfResults.Where(r => r.Success).Select(r => r.ToString());
					result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(), specType.Name,
						$"oneOf more than one successful failure: {string.Join(",", errors)}"));
				}
			}
		}

		private static void ValidateProperties(Type specType, JsonSchema schema, ImplementationProperty[] properties, TypeValidationResult result)
		{
			IReadOnlyDictionary<string, JsonSchemaProperty> schemaProperties;

			try
			{
				schemaProperties = schema.ActualProperties;
			}
			catch (InvalidOperationException e)
			{
				throw new JsonSchemaException($"Cannot get schema properties for {schema.GetNameOrSpecificationId()}. {e.Message}", e);
			}

			foreach (var kv in schemaProperties)
			{
				var schemaProperty = kv.Value.ActualSchema;
				var name = kv.Value.Name;
				var specTypeProperty = properties.SingleOrDefault(p => p.Name == name);
				if (specTypeProperty == null)
				{
					if (schemaProperty.Type.HasFlag(JsonObjectType.Null))
					{
						if (result.Validation == Validation.SpecToType)
							result.AddError(TypeValidationError.NotFound(specType, schema.GetNameOrSpecificationId(), name));
					}
					else
						result.AddError(TypeValidationError.NotFound(specType, schema.GetNameOrSpecificationId(), name));

					continue;
				}

				// check certain .NET types first before defaulting to the JSON schema flags.
				// A property might be represented as more than one type e.g. ["integer", "string", "null],
				// so it's better to look at the .NET type first and try to look for the associated JSON schema flag.
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
							ValidateNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else
						{
							result.AddError(
								TypeValidationError.ExpectedType(
									specType,
									specTypeProperty.PropertyType,
									"number",
									schema.GetNameOrSpecificationId(),
									name));
						}
						break;
					case "System.Int64":
					case "System.Int32":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Number))
							ValidateNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Integer))
							ValidateInteger(specType, schema, schemaProperty, specTypeProperty, result);
						else
						{
							result.AddError(
								TypeValidationError.ExpectedType(
									specType,
									specTypeProperty.PropertyType,
									"integer",
									schema.GetNameOrSpecificationId(),
									name));
						}
						break;
					case "System.String":
						if (schemaProperty.Type.HasFlag(JsonObjectType.String))
							ValidateString(specType, schema, schemaProperty, specTypeProperty, result);
						else
						{
							result.AddError(
								TypeValidationError.ExpectedType(
									specType,
									specTypeProperty.PropertyType,
									"string",
									schema.GetNameOrSpecificationId(),
									name));
						}
						break;
					case "System.Boolean":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Boolean))
							ValidateBoolean(specType, schema, schemaProperty, specTypeProperty, result);
						else
						{
							result.AddError(
								TypeValidationError.ExpectedType(
									specType,
									specTypeProperty.PropertyType,
									"boolean",
									schema.GetNameOrSpecificationId(),
									name));
						}
						break;
					default:
						// Are there multiple types? If so, based on the .NET type not being a primitive type, we would expect the presence of the
						// schema "object" or "array" type in the majority of cases, so check these first.
						// For types with custom serialization, the schema type may be a primitive type, so we can't easily statically validate it.
						if (HasMultipleNonNullTypes(schemaProperty.Type))
						{
							if (schemaProperty.Type.HasFlag(JsonObjectType.Object))
								ValidateProperties(specTypeProperty.PropertyType, schemaProperty, result);
							else if (schemaProperty.Type.HasFlag(JsonObjectType.Array) &&
								IsEnumerableType(specTypeProperty.PropertyType, out var elementType))
								ValidateProperties(elementType, schemaProperty.Item.ActualSchema, result);
							else
							{
								result.AddIgnore(new TypeValidationIgnore(schema.GetNameOrSpecificationId(), name,
									$"Cannot statically check type. .NET type '{specType}', schema type '{schemaProperty.Type}'"));
							}
						}
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Boolean))
							ValidateBoolean(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Integer))
							ValidateInteger(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Number))
							ValidateNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.String))
						{
							if (schemaProperty.IsEnumeration && specTypeProperty.PropertyType.IsEnum)
								ValidateEnum(specType, schema, schemaProperty, specTypeProperty, result);
							else
								ValidateString(specType, schema, schemaProperty, specTypeProperty, result);
						}
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Object))
							ValidateProperties(specTypeProperty.PropertyType, schemaProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Array))
						{
							if (IsEnumerableType(specTypeProperty.PropertyType, out var elementType))
								ValidateProperties(elementType, schemaProperty.Item.ActualSchema, result);
						}
						break;
				}
			}
		}

		/// <summary>
		/// Check if the JSON schema type has more than one value, other than "null"
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static bool HasMultipleNonNullTypes(JsonObjectType type)
		{
			var typeFlags = type;

			// remove null flag if it exists, and check to see if we have more than one flag
			if (typeFlags.HasFlag(JsonObjectType.Null))
				typeFlags &= ~JsonObjectType.Null;

			// is there still more than one flag? Check to see if it's a power of two
			return (typeFlags & (typeFlags - 1)) != 0;
		}

		private static void ValidateEnum(Type specType, JsonSchema schema,
			JsonSchema schemaProperty, ImplementationProperty specTypeProperty, TypeValidationResult result
		)
		{
			var enumValues = GetEnumValues(specTypeProperty.PropertyType);
			foreach (var enumValue in enumValues)
			{
				if (!schemaProperty.Enumeration.Cast<string>().Contains(enumValue))
				{
					result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(),
						schemaProperty.GetNameOrSpecificationId(), $"enum did not contain value {enumValue}"));
				}
			}
		}

		private static IEnumerable<string> GetEnumValues(Type enumType)
		{
			var values = Enum.GetValues(enumType);
			for (var i = 0; i < values.Length; i++)
			{
				var value = values.GetValue(i);
				var info = enumType.GetField(value.ToString());
				var da = info.GetCustomAttribute<EnumMemberAttribute>();
				var enumValue = da != null ? da.Value : Enum.GetName(enumType, value);
				yield return enumValue;
			}
		}

		private static void ValidateString(Type specType, JsonSchema schema, JsonSchema property, ImplementationProperty specTypeProperty,
			TypeValidationResult result
		)
		{
			CheckType(specType, "string", schema, property, specTypeProperty, result, (t, s) => (t == typeof(string)));
			CheckMaxLength(specType, schema, property, specTypeProperty, result);
		}

		private static void ValidateBoolean(Type specType, JsonSchema schema, JsonSchema property, ImplementationProperty specTypeProperty,
			TypeValidationResult result
		) =>
			CheckType(specType, "boolean", schema, property, specTypeProperty, result, (t, s) => t == typeof(bool));

		private static void ValidateInteger(Type specType, JsonSchema schema, JsonSchema property, ImplementationProperty specTypeProperty,
			TypeValidationResult result
		) =>
			CheckType(specType, "integer", schema, property, specTypeProperty, result, (t, s) => t == typeof(int) || t == typeof(long));

		private static void ValidateNumber(Type specType, JsonSchema schema, JsonSchema property, ImplementationProperty specTypeProperty,
			TypeValidationResult result
		) =>
			CheckType(specType, "number", schema, property, specTypeProperty, result, (t, s) => NumericTypeNames.Contains(t.FullName));

		private static void CheckType(Type specType, string expectedType, JsonSchema schema, JsonSchema property,
			ImplementationProperty implementationProperty,
			TypeValidationResult result, Func<Type, JsonSchema, bool> typeCheck
		)
		{
			var propertyType = implementationProperty.PropertyType;
			var nullable = !propertyType.IsValueType;

			if (IsNullableType(propertyType))
			{
				nullable = true;
				propertyType = Nullable.GetUnderlyingType(implementationProperty.PropertyType);
			}

			if (!typeCheck(propertyType, property))
				result.AddError(TypeValidationError.ExpectedType(specType, propertyType, expectedType, schema.GetNameOrSpecificationId(),
					property.GetNameOrSpecificationId()));

			if (result.Validation == Validation.SpecToType && property.Type.HasFlag(JsonObjectType.Null) && !nullable)
				result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(), property.GetNameOrSpecificationId(),
					"expected type to be nullable"));
		}

		private static void CheckMaxLength(Type specType, JsonSchema schema, JsonSchema schemaProperty, ImplementationProperty implementationProperty,
			TypeValidationResult result
		)
		{
			if (schemaProperty.MaxLength.HasValue)
			{
				var maxLength = schemaProperty.MaxLength.Value;
				if (!implementationProperty.MaxLength.HasValue)
					result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(), implementationProperty.Name,
						$"expected property to enforce maxLength of {maxLength} but does not"));
				else
				{
					if (implementationProperty.MaxLength != maxLength)
						result.AddError(new TypeValidationError(specType, schema.GetNameOrSpecificationId(), implementationProperty.Name,
							$"property max length {implementationProperty.MaxLength} not equal to spec maxLength {maxLength}"));
				}
			}
		}

		private static bool IsNullableType(Type type) =>
			type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

		/// <summary>
		/// Check if type implements <see cref="IEnumerable{T}"/>
		/// </summary>
		/// <param name="type">The type to check</param>
		/// <param name="elementType">The type of T in <see cref="IEnumerable{T}"/> if the type implements <see cref="IEnumerable{T}"/></param>
		/// <returns><c>true</c> if type implements <see cref="IEnumerable{T}"/>, <c>false</c> otherwise</returns>
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
		public ContractResolveException(string message) : base(message) { }
	}

	public class JsonSchemaException : Exception
	{
		public JsonSchemaException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
