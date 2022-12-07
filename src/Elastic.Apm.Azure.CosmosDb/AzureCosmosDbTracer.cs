// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Model;

namespace Elastic.Apm.Azure.CosmosDb
{
	internal class AzureCosmosDbTracer : IHttpSpanTracer
	{
		private const string CreateOrQueryDocumentOperationName = "Create/query document";
		private static readonly char[] RequestPathEndDelimiters = { '?', '#' };
		private static readonly char[] RequestPathTokenDelimiters = { '/' };

		public bool IsMatch(string method, Uri requestUrl, Func<string, string> headerGetter) =>
			requestUrl.Host.EndsWith(".documents.azure.com", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".documents.usgovcloudapi.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".documents.chinacloudapi.cn", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".documents.cloudapi.de", StringComparison.Ordinal);

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

		public ISpan StartSpan(IApmAgent agent, string method, Uri requestUrl, Func<string, string> headerGetter)
		{
			var resourcePath = ParseResourcePath(requestUrl.PathAndQuery);
			var operation = BuildOperationMoniker(method, resourcePath);
			var operationName = GetOperationName(operation);

			var spanName = operationName.Length > 4
				? $"Cosmos DB {operationName}"
				: "Cosmos DB";

			var span = ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(agent, spanName, ApiConstants.TypeDb, ApiConstants.SubTypeCosmosDb, isExitSpan: true);

			if (span != null)
			{
				span.Context.Db = new Database();
				foreach (var resource in resourcePath)
				{
					if (resource.Value != null)
					{
						if (resource.Key == "dbs")
							span.Context.Db.Instance = resource.Value;

						var propertyName = GetPropertyNameForResource(resource.Key);
						if (!string.IsNullOrEmpty(propertyName))
							span.Name += $" {resource.Value}";
					}
				}
			}

			return span;
		}

		public bool ShouldSuppressSpanCreation() => false;

		private static string GetOperationName(string operation) =>
			OperationNames.TryGetValue(operation, out var operationName) ? operationName : operation;

		/// <summary>
		/// Builds a resource operation moniker in the format of "VERB /a/*/b/*/c".
		/// </summary>
		/// <param name="verb">The HTTP verb.</param>
		/// <param name="resourcePath">The resource path represented as a list of resource type and resource ID pairs.</param>
		/// <returns>Operation moniker string.</returns>
		internal static string BuildOperationMoniker(string verb, List<KeyValuePair<string, string>> resourcePath)
		{
			var tokens = new List<string>((4 * resourcePath.Count) + 2);

			if (!string.IsNullOrEmpty(verb))
			{
				tokens.Add(verb);
				tokens.Add(" ");
			}

			foreach (var resource in resourcePath)
			{
				tokens.Add("/");
				tokens.Add(resource.Key);
				if (resource.Value != null)
					tokens.Add("/*");
			}

			return string.Concat(tokens);
		}

		/// <summary>
		/// Parses request path into REST resource path represented as a list of resource type and resource ID pairs.
		/// </summary>
		/// <param name="requestPath">The request path.</param>
		/// <returns>A list of resource type and resource ID pairs.</returns>
		internal static List<KeyValuePair<string, string>> ParseResourcePath(string requestPath)
		{
			var tokens = TokenizeRequestPath(requestPath);

			var pairCount = (tokens.Count + 1) / 2;
			var results = new List<KeyValuePair<string, string>>(pairCount);
			for (var i = 0; i < pairCount; i++)
			{
				var keyIdx = 2 * i;
				var valIdx = keyIdx + 1;
				var key = tokens[keyIdx];
				var value = valIdx == tokens.Count ? null : tokens[valIdx];
				if (!string.IsNullOrEmpty(key))
					results.Add(new KeyValuePair<string, string>(key, value));
			}

			return results;
		}

		/// <summary>
		/// Tokenizes request path.
		/// E.g. the string "/a/b/c/d?e=f" will be tokenized into [ "a", "b", "c", "d" ].
		/// </summary>
		/// <param name="requestPath">The request path.</param>
		/// <returns>List of tokens.</returns>
		internal static List<string> TokenizeRequestPath(string requestPath)
		{
			var slashPrefixShift = requestPath[0] == '/' ? 1 : 0;
			var endIdx = requestPath.IndexOfAny(RequestPathEndDelimiters, slashPrefixShift);
			var tokens = Split(requestPath, RequestPathTokenDelimiters, slashPrefixShift, endIdx);

			return tokens;
		}

		/// <summary>
		/// Splits substring by given delimiters.
		/// </summary>
		/// <param name="str">The string to split.</param>
		/// <param name="delimiters">The delimiters.</param>
		/// <param name="startIdx">
		/// The index at which splitting will start.
		/// This is not validated and expected to be within input string range.
		/// </param>
		/// <param name="endIdx">
		/// The index at which splitting will end.
		/// If -1 then string will be split till it's end.
		/// This is not validated and expected to be less than string length.
		/// </param>
		/// <returns>A list of substrings.</returns>
		internal static List<string> Split(string str, char[] delimiters, int startIdx, int endIdx)
		{
			if (endIdx < 0)
				endIdx = str.Length;

			if (endIdx <= startIdx)
				return new List<string>();

			var results = new List<string>(16);

			var idx = startIdx;
			while (idx <= endIdx)
			{
				var cutIdx = str.IndexOfAny(delimiters, idx, endIdx - idx);
				if (cutIdx < 0)
					cutIdx = endIdx;

				results.Add(str.Substring(idx, cutIdx - idx));
				idx = cutIdx + 1;
			}

			return results;
		}
	}
}
