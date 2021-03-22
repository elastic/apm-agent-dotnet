// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.DiagnosticListeners.Parsers
{
	using System.Collections.Generic;
	using Elastic.Apm.Api;
	using Elastic.Apm.Logging;

	/// <summary>
	/// HTTP Dependency parser that attempts to parse dependency as Azure DocumentDB call.
	/// </summary>
	internal static class DocumentDbHttpParser
	{
		private const string CreateOrQueryDocumentOperationName = "Create/query document";

		private static readonly string[] DocumentDbHostSuffixes =
			{
				".documents.azure.com",
				".documents.chinacloudapi.cn",
				".documents.cloudapi.de",
				".documents.usgovcloudapi.net",
			};

		private static readonly string[] DocumentDbSupportedVerbs = { "GET", "POST", "PUT", "HEAD", "DELETE" };

		private static readonly Dictionary<string, string> OperationNames = new Dictionary<string, string>
		{
			// Database operations
			["POST /dbs"] = "Create database",
			["GET /dbs"] = "List databases",
			["GET /dbs/*"] = "Get database",
			["DELETE /dbs/*"] = "Delete database",

			// Collection operations
			["POST /dbs/*/colls"] = "Create collection",
			["GET /dbs/*/colls"] = "List collections",
			["GET /dbs/*/colls/*"] = "Get collection",
			["DELETE /dbs/*/colls/*"] = "Delete collection",
			["PUT /dbs/*/colls/*"] = "Replace collection",

			// Document operations
			["POST /dbs/*/colls/*/docs"] = CreateOrQueryDocumentOperationName, // Create & Query share this moniker
			["GET /dbs/*/colls/*/docs"] = "List documents",
			["GET /dbs/*/colls/*/docs/*"] = "Get document",
			["PUT /dbs/*/colls/*/docs/*"] = "Replace document",
			["DELETE /dbs/*/colls/*/docs/*"] = "Delete document",

			// partition key operations
			["POST /dbs/*/colls/*/pkranges"] = "Create partition key ranges",
			["GET /dbs/*/colls/*/pkranges"] = "List partition key ranges",
			["GET /dbs/*/colls/*/pkranges"] = "Get partition key ranges",
			["PUT /dbs/*/colls/*/pkranges"] = "Replace partition key ranges",
			["DELETE /dbs/*/colls/*/pkranges"] = "Delete partition key ranges",

			// Attachment operations
			["POST /dbs/*/colls/*/docs/*/attachments"] = "Create attachment",
			["GET /dbs/*/colls/*/docs/*/attachments"] = "List attachments",
			["GET /dbs/*/colls/*/docs/*/attachments/*"] = "Get attachment",
			["PUT /dbs/*/colls/*/docs/*/attachments/*"] = "Replace attachment",
			["DELETE /dbs/*/colls/*/docs/*/attachments/*"] = "Delete attachment",

			// Stored procedure operations
			["POST /dbs/*/colls/*/sprocs"] = "Create stored procedure",
			["GET /dbs/*/colls/*/sprocs"] = "List stored procedures",
			["PUT /dbs/*/colls/*/sprocs/*"] = "Replace stored procedure",
			["DELETE /dbs/*/colls/*/sprocs/*"] = "Delete stored procedure",
			["POST /dbs/*/colls/*/sprocs/*"] = "Execute stored procedure",

			// User defined function operations
			["POST /dbs/*/colls/*/udfs"] = "Create UDF",
			["GET /dbs/*/colls/*/udfs"] = "List UDFs",
			["PUT /dbs/*/colls/*/udfs/*"] = "Replace UDF",
			["DELETE /dbs/*/colls/*/udfs/*"] = "Delete UDF",

			// Trigger operations
			["POST /dbs/*/colls/*/triggers"] = "Create trigger",
			["GET /dbs/*/colls/*/triggers"] = "List triggers",
			["PUT /dbs/*/colls/*/triggers/*"] = "Replace trigger",
			["DELETE /dbs/*/colls/*/triggers/*"] = "Delete trigger",

			// User operations
			["POST /dbs/*/users"] = "Create user",
			["GET /dbs/*/users"] = "List users",
			["GET /dbs/*/users/*"] = "Get user",
			["PUT /dbs/*/users/*"] = "Replace user",
			["DELETE /dbs/*/users/*"] = "Delete user",

			// Permission operations
			["POST /dbs/*/users/*/permissions"] = "Create permission",
			["GET /dbs/*/users/*/permissions"] = "List permissions",
			["GET /dbs/*/users/*/permissions/*"] = "Get permission",
			["PUT /dbs/*/users/*/permissions/*"] = "Replace permission",
			["DELETE /dbs/*/users/*/permissions/*"] = "Delete permission",

			// Offer operations
			["POST /offers"] = "Query offers",
			["GET /offers"] = "List offers",
			["GET /offers/*"] = "Get offer",
			["PUT /offers/*"] = "Replace offer",
		};

		/// <summary>
		/// Tries parsing URL info. If the url matches well known Cosmos Db urls, it'll create a span with the collected information.
		/// </summary>
		/// <param name="host">Host of the URL</param>
		/// <param name="pathAndQuery">The path and query string part of the URL</param>
		/// <param name="verb">The HTTP verb for the HTTP request</param>
		/// <param name="tracer">A tracer to start the span</param>
		/// <param name="logger">A logger to log</param>
		/// <returns>A span populated with CosmosDB info if successfully parsed the URL, <code>null</code> otherwise</returns>
		internal static ISpan TryCreateCosmosDbSpan(string host, string pathAndQuery, string verb, ITracer tracer, IApmLogger logger)
		{

			if (host == null )
			{
				return null;
			}

			if (!HttpParsingHelper.EndsWithAny(host, DocumentDbHostSuffixes))
			{
				return null;
			}

			////
			//// DocumentDB REST API: https://docs.microsoft.com/en-us/rest/api/documentdb/
			////

			logger.Debug()?.Log("Host of the HTTP Request matches Cosmos DB - start parsing URL");

			var resourcePath = HttpParsingHelper.ParseResourcePath(pathAndQuery);
		
			var operation = HttpParsingHelper.BuildOperationMoniker(verb, resourcePath);
			var operationName = GetOperationName(null, operation);

			var spanName = "Cosmos DB" + (operationName.Length > 4 ? (" " + operationName) : string.Empty);
			var span = tracer.CurrentTransaction?.StartSpan(spanName, ApiConstants.TypeDb, "CosmosDb");

			if (span != null)
			{
				span.Type = ApiConstants.TypeDb;
				span.Subtype = ApiConstants.SubTypeCosmosDb;
				span.Context.Db = new Database();

				foreach (var resource in resourcePath)
				{
					if (resource.Value != null)
					{
						if (resource.Key == "dbs")
						{
							span.Context.Db.Instance = resource.Value;
						}

						var propertyName = GetPropertyNameForResource(resource.Key);

						if (!string.IsNullOrEmpty(propertyName))
							span.Name += $" {resource.Value}";

					}
				}
			}

			return span;
		}

		private static string GetPropertyNameForResource(string resourceType)
		{
			// ignore high cardinality resources (documents, attachments, etc.)
			switch (resourceType)
			{
				case "dbs":
					return "Database";
				case "colls":
					return "Collection";
				case "sprocs":
					return "Stored procedure";
				case "udfs":
					return "User defined function";
				case "triggers":
					return "Trigger";
				default:
					return null;
			}
		}

		private static string GetOperationName(string resultCode, string operation)
		{
			if (!OperationNames.TryGetValue(operation, out var operationName))
			{
				return operation;
			}

			return operationName;
		}
	}
}
