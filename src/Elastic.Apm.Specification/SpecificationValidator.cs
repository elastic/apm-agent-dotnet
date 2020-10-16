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
	/// <summary>
	/// Validates types against the APM server specification
	/// </summary>
	/// <remarks>
	///	Not thread-safe when more than one instance points to the same directory because
	/// directories and files are deleted and created as needed.
	/// </remarks>
	public class SpecificationValidator
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

		public SpecificationValidator(string branch, string directory)
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
		///
		/// </summary>
		/// <param name="specificationId"></param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">
		/// Spec file does not exist
		/// </exception>
		public async Task<JsonSchema> LoadSchemaAsync(string specificationId)
		{
			var branchDirectory = Path.Combine(_directory, Branch);
			var path = Path.Combine(branchDirectory, specificationId);

			if (!File.Exists(path))
				throw new FileNotFoundException($"'{specificationId}' does not exist at {path}", path);

			return await JsonSchema.FromFileAsync(path).ConfigureAwait(false);
		}

		/// <summary>
		/// /// <summary>
		/// Validates the specification against the type. A specification may be more lenient, or define more
		/// properties than are specified on a type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="Exception">
		/// A type implements more than one spec
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// Spec file does not exist
		/// </exception>
		/// </summary>
		public async Task<ValidationResult> ValidateSpecAgainstTypeAsync(Type type)
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

			var specificationId = specAttributes.Single().Path;
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
				// an object contract and won't be able to determine validity of the type to the schema through reflection.
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
				result.AddIgnore(new ValidationIgnore(schema.DocumentPath, specType.Name, e.Message));
				return;
			}

			foreach (var kv in schema.ActualProperties)
			{
				JsonSchema schemaProperty = kv.Value;
				var name = kv.Value.Name;
				var specTypeProperty = properties.SingleOrDefault(p => p.Name == name);
				if (specTypeProperty == null)
				{
					if (schemaProperty.Type.HasFlag(JsonObjectType.None))
					{
						schemaProperty = schemaProperty.ActualSchema;
					}

					if (!schemaProperty.Type.HasFlag(JsonObjectType.Null))
						result.AddError(ValidationError.NotFound(specType, schema.DocumentPath, name));

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
							result.AddError(new ValidationError(specType, schema.DocumentPath, name, $"expecting 'number' type but found {schemaProperty.Type}"));
						break;
					case "System.Int64":
					case "System.Int32":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Number))
							CheckNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Integer))
							CheckInteger(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.DocumentPath, name, $"expecting 'number' or 'integer' type but found {schemaProperty.Type}"));
						break;
					case "System.String":
						if (schemaProperty.Type.HasFlag(JsonObjectType.String))
							CheckString(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.DocumentPath, name, $"expecting 'string' type but found {schemaProperty.Type}"));
						break;
					case "System.Boolean":
						if (schemaProperty.Type.HasFlag(JsonObjectType.Boolean))
							CheckBoolean(specType, schema, schemaProperty, specTypeProperty, result);
						else
							result.AddError(new ValidationError(specType, schema.DocumentPath, name, $"expecting 'boolean' type but found {schemaProperty.Type}"));
						break;
					default:
						if (schemaProperty.Type.HasFlag(JsonObjectType.Boolean))
							CheckBoolean(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Integer))
							CheckInteger(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Number))
							CheckNumber(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.String))
							CheckString(specType, schema, schemaProperty, specTypeProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Object))
							ValidateSpecProperties(specTypeProperty.PropertyType, schemaProperty, result);
						else if (schemaProperty.Type.HasFlag(JsonObjectType.Array))
						{
							if (IsEnumerableType(specTypeProperty.PropertyType, out var elementType))
								ValidateSpecProperties(elementType, schemaProperty.Item, result);
						}
						break;
				}
			}

			// TODO: handle AnyOf, AllOf, OneOf
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
				result.AddError(ValidationError.ExpectedType(specType, expectedType, schema.DocumentPath, specificationProperty.Name, propertyType.FullName));

			if (property.Type.HasFlag(JsonObjectType.Null) && !nullable)
				result.AddError(new ValidationError(specType, schema.DocumentPath, property.Name, "expected type to be nullable"));
		}

		private static void CheckMaxLength(Type specType, JsonSchema schema, JsonSchema schemaProperty, SpecificationProperty specificationProperty,
			ValidationResult result
		)
		{
			if (schemaProperty.MaxLength.HasValue)
			{
				var maxLength = schemaProperty.MaxLength.Value;
				if (!specificationProperty.MaxLength.HasValue)
					result.AddError(new ValidationError(specType, schema.DocumentPath, specificationProperty.Name, $"expected property to enforce maxLength of {maxLength} but does not"));
				else
				{
					if (specificationProperty.MaxLength != maxLength)
						result.AddError(new ValidationError(specType, schema.DocumentPath, specificationProperty.Name,
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
