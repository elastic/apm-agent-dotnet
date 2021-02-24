﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Apm.DiagnosticListeners.Parsers
{

	using System.Collections.Generic;
	using Elastic.Apm.Api;

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
		/// Tries parsing given dependency telemetry item. 
		/// </summary>
		/// <param name="httpDependency">Dependency item to parse. It is expected to be of HTTP type.</param>
		/// <returns><code>true</code> if successfully parsed dependency.</returns>
		internal static ISpan TryCreateCosmosDbSpan(string host, string fullUrl, string pathAndQuery, ITracer tracer)
		{
			
			//var url = iUrl;// httpDependency.Data;

			if (/*name == null || */ host == null || fullUrl == null)
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


			// try to parse out the verb
		//	HttpParsingHelper.ExtractVerb(name, out var verb, out var nameWithoutVerb, DocumentDbSupportedVerbs);

			var resourcePath = HttpParsingHelper.ParseResourcePath(pathAndQuery);

			var span = tracer.CurrentTransaction?.StartSpan("CosmosDb", ApiConstants.TypeDb, "CosmosDb");

			// populate properties
			foreach (var resource in resourcePath)
			{
				if (resource.Value != null)
				{
					var propertyName = GetPropertyNameForResource(resource.Key);
					if (propertyName != null)
					{
						//httpDependency.Properties[propertyName] = resource.Value;
					}
				}
			}

			if (span != null)
			{
				span.Context.Db = new Database();

				foreach (var resource in resourcePath)
				{
					if (resource.Value != null)
					{
						if(resource.Key == "db")
						{
							span.Context.Db.Instance = resource.Value;
						}
						else
						{
							span.SetLabel(resource.Key, resource.Value);
						}
					}
				}
			}

			//var operation = HttpParsingHelper.BuildOperationMoniker(verb, resourcePath);
			//var operationName = GetOperationName(resultCode, operation);

			//httpDependency.Type = "CosmosDb"; // RemoteDependencyConstants.AzureDocumentDb;
			//httpDependency.Name = string.IsNullOrEmpty(operationName) ? httpDependency.Target : operationName;

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

			// "Create document" and "Query documents" share the same moniker
			// but we can try to distinguish them by response code
			if (operationName == CreateOrQueryDocumentOperationName)
			{
				switch (resultCode)
				{
					case "200":
						{
							operationName = "Query documents";
							break;
						}

					case "201":
					case "403":
					case "409":
					case "413":
						{
							operationName = "Create document";
							break;
						}
				}
			}

			return operationName;
		}
	}

}
