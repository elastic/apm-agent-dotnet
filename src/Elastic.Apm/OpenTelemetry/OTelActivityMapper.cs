// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET || NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.OpenTelemetry
{
	/// <summary>
	/// Translates OpenTelemetry <see cref="Activity"/> attributes into Elastic APM model fields.
	/// </summary>
	internal static class OTelActivityMapper
	{
		internal static readonly string[] ServerPortAttributeKeys =
			[SemanticConventions.ServerPort, SemanticConventions.NetworkPeerPort, SemanticConventions.NetPeerPort];

		internal static readonly string[] ServerAddressAttributeKeys =
			[SemanticConventions.ServerAddress, SemanticConventions.NetworkPeerAddress, SemanticConventions.NetPeerName, SemanticConventions.NetPeerIp];

		// Canonical URL/host-presence keys used both as "is this HTTP?" and for URL parsing.
		internal static readonly string[] HttpAttributeKeys =
			[SemanticConventions.UrlFull, SemanticConventions.HttpUrl];

		internal static readonly string[] DbSystemAttributeKeys =
			[SemanticConventions.DbSystemName, SemanticConventions.DbSystem];

		internal static readonly string[] DbInstanceAttributeKeys =
			[SemanticConventions.DbNamespace, SemanticConventions.DbName];

		internal static readonly string[] DbQueryTextAttributeKeys =
			[SemanticConventions.DbQueryText, SemanticConventions.DbStatement];

		// Systems where db.namespace is the confirmed stable replacement for db.name and maps correctly
		// to span.db.instance / service.target.name. Validated against each system's OTel spec page:
		//   mongodb   — db.namespace = database name
		//   mysql     — db.namespace = database name
		//   cassandra — db.namespace = keyspace (the Cassandra equivalent of a database)
		//   cosmosdb  — db.namespace = database name ("azure.cosmosdb" is normalized to "cosmosdb" before this check)
		// Excluded systems and why:
		//   elasticsearch — db.namespace = cluster name, not the index/alias being accessed
		//   postgresql    — db.namespace = "{database}|{schema}", composite; changes service target grouping vs db.name
		//   mssql         — db.namespace = "{instance}|{database}", composite; same concern as postgresql
		// All other systems use db.name only.
		internal static readonly HashSet<string> DbNamespaceAsInstanceSystems =
			new(StringComparer.OrdinalIgnoreCase) { "mongodb", "mysql", "cassandra", "cosmosdb" };

		internal static void UpdateOTelAttributes(Activity activity, OTel otel)
		{
			var i = 0;
			foreach (var tagObject in activity.TagObjects)
			{
				if (i >= 128)
				{
					// https://opentelemetry.io/docs/specs/otel/common/#attribute-limits
					// copy max 128 keys and truncate values to 10k chars (the current maximum for e.g. statement.db).
					break;
				}

				otel.Attributes ??= [];

				if (tagObject.Value is string s)
					otel.Attributes[tagObject.Key] = s.Truncate(10_000);
				else
					otel.Attributes[tagObject.Key] = tagObject.Value;
				i++;
			}
		}

		internal static void InferTransactionType(Transaction transaction, Activity activity)
		{
			if (activity.Kind == ActivityKind.Server && (TryGetStringValue(activity, SemanticConventions.RpcSystem, out _)
					|| TryGetStringValue(activity, HttpAttributeKeys, out _)
					|| TryGetStringValue(activity, SemanticConventions.HttpScheme, out _)))
				transaction.Type = ApiConstants.TypeRequest;
			else if (activity.Kind == ActivityKind.Consumer && TryGetStringValue(activity, SemanticConventions.MessagingSystem, out _))
				transaction.Type = ApiConstants.TypeMessaging;
			else
				transaction.Type = "unknown";
		}

		internal static void InferSpanTypeAndSubType(Span span, Activity activity)
		{
			var peerPort = string.Empty;
			var peerAddress = string.Empty;

			if (TryGetStringValue(activity, ServerPortAttributeKeys, out var netPortValue))
				peerPort = netPortValue;

			if (TryGetStringValue(activity, ServerAddressAttributeKeys, out var netNameValue))
				peerAddress = netNameValue;

			var netName = peerAddress;
			if (netName.Length > 0 && peerPort.Length > 0)
			{
				netName += ':';
				netName += peerPort;
			}

			string serviceTargetType = null;
			string serviceTargetName = null;
			string resource = null;

			// db.system.name is the current OTel convention; db.system is the older one still used by many libraries.
			if (TryGetStringValue(activity, DbSystemAttributeKeys, out var dbSystem))
			{
				// Normalize OTel system names to the Elastic APM subtype values expected by APM server / Kibana.
				// The raw OTel value is preserved in otel.attributes; this only affects ECS-mapped fields.
				dbSystem = NormalizeDbSystem(dbSystem);
				span.Type = ApiConstants.TypeDb;
				span.Subtype = dbSystem;
				span.Action = ApiConstants.ActionQuery;
				serviceTargetType = span.Subtype;

				// db.namespace semantics vary by system — only use it for systems where it is a direct
				// equivalent of the database name / instance concept (see DbNamespaceAsInstanceSystems).
				// For others (e.g. elasticsearch = cluster name, postgresql = "{db}|{schema}") fall back
				// to db.name only, to avoid incorrect service target grouping.
				string dbInstance = null;
				if (DbNamespaceAsInstanceSystems.Contains(dbSystem))
					TryGetStringValue(activity, DbInstanceAttributeKeys, out dbInstance);
				else
					TryGetStringValue(activity, SemanticConventions.DbName, out dbInstance);
				serviceTargetName = dbInstance;
				resource = ToResourceName(span.Subtype, serviceTargetName);

				// db.query.text is the current OTel convention; db.statement is the older one.
				TryGetStringValue(activity, DbQueryTextAttributeKeys, out var dbStatement);
				span.Context.Db = new Database
				{
					Type = dbSystem,
					Instance = dbInstance,
					Statement = dbStatement
				};
			}
			else if (TryGetStringValue(activity, SemanticConventions.MessagingSystem, out var messagingSystem))
			{
				span.Type = ApiConstants.TypeMessaging;
				span.Subtype = messagingSystem;
				serviceTargetType = span.Subtype;
				serviceTargetName = TryGetStringValue(activity, SemanticConventions.MessagingDestination, out var messagingDestination)
					? messagingDestination
					: null;
				resource = ToResourceName(span.Subtype, serviceTargetName);
			}
			else if (TryGetStringValue(activity, SemanticConventions.RpcSystem, out var rpcSystem))
			{
				span.Type = ApiConstants.TypeExternal;
				span.Subtype = rpcSystem;
				serviceTargetType = span.Subtype;
				serviceTargetName = !string.IsNullOrEmpty(netName)
					? netName
					: TryGetStringValue(activity, SemanticConventions.RpcService, out var rpcService)
						? rpcService
						: null;
				resource = serviceTargetName ?? span.Subtype;
			}
			else if (TryGetStringValue(activity, HttpAttributeKeys, out var httpUrl)
				|| TryGetStringValue(activity, SemanticConventions.HttpScheme, out _))
			{
				var hasHttpHost = TryGetStringValue(activity, SemanticConventions.HttpHost, out var httpHost);
				var hasHttpScheme = TryGetStringValue(activity, SemanticConventions.HttpScheme, out var httpScheme);
				span.Type = ApiConstants.TypeExternal;
				span.Subtype = ApiConstants.SubtypeHttp;
				serviceTargetType = span.Subtype;
				if (hasHttpHost && hasHttpScheme)
				{
					var httpPort = HttpPortFromScheme(httpScheme);
					serviceTargetName = string.IsNullOrEmpty(httpPort) ? httpHost : $"{httpHost}:{httpPort}";
				}
				else if (!string.IsNullOrEmpty(httpUrl))
				{
					var parsedNetName = ParseNetName(httpUrl);
					serviceTargetName = string.IsNullOrEmpty(parsedNetName) ? null : parsedNetName;
				}
				else
					serviceTargetName = string.IsNullOrEmpty(netName) ? null : netName;

				resource = string.IsNullOrEmpty(serviceTargetName) ? null : serviceTargetName;
			}

			if (serviceTargetType == null)
			{
				if (activity.Kind == ActivityKind.Internal)
				{
					span.Type = ApiConstants.TypeApp;
					span.Subtype = ApiConstants.SubTypeInternal;
				}
				else
					span.Type = ApiConstants.TypeUnknown;
			}

			span.Context.Service = new SpanService(new Target(serviceTargetType, serviceTargetName));
			if (resource != null)
			{
				span.Context.Destination ??= new Destination();
				span.Context.Destination.Service = new Destination.DestinationService { Resource = resource };
			}
			if (peerAddress.Length > 0)
			{
				span.Context.Destination ??= new Destination();
				span.Context.Destination.Address = peerAddress;
				if (peerPort.Length > 0 && int.TryParse(peerPort, out var parsedPort))
					span.Context.Destination.Port = parsedPort;
			}
		}

		/// <summary>
		/// Reads an activity tag as a string, coercing <see cref="int"/> and <see cref="long"/> numeric
		/// values (which OTel instrumentation libraries commonly use for port numbers).
		/// Returns <c>false</c> for absent keys or values of any other type.
		/// </summary>
		internal static bool TryGetStringValue(Activity activity, string key, out string value)
		{
			value = null;

#if NET
			var attribute = activity.GetTagItem(key);
#else
			var attribute = activity.TagObjects.FirstOrDefault(kvp => kvp.Key == key).Value;
#endif

			if (attribute is string stringValue)
			{
				value = stringValue;
				return true;
			}

			if (attribute is int intValue)
			{
				value = intValue.ToString();
				return true;
			}

			if (attribute is long longValue)
			{
				value = longValue.ToString();
				return true;
			}

			return false;
		}

		internal static bool TryGetStringValue(Activity activity, string[] keys, out string value)
		{
			value = null;

			foreach (var key in keys)
			{
				if (TryGetStringValue(activity, key, out var attributeValue))
				{
					value = attributeValue;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns the well-known default port string for <c>http</c> / <c>https</c> schemes,
		/// or <c>null</c> for any other scheme (preventing a trailing-colon resource name).
		/// </summary>
		private static string HttpPortFromScheme(string scheme) => scheme switch
		{
			"http"  => "80",
			"https" => "443",
			_       => null
		};

		/// <summary>
		/// Extracts <c>host</c> or <c>host:port</c> from a URL string.
		/// Returns an empty string if the URL cannot be parsed.
		/// </summary>
		private static string ParseNetName(string url)
		{
			try
			{
				var u = new Uri(url); // https://developer.mozilla.org/en-US/docs/Web/API/URL
				// Uri.Port returns -1 when no port is present and the scheme has no default.
				return u.Port > 0 ? u.Host + ':' + u.Port : u.Host;
			}
			catch (UriFormatException)
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Maps OTel <c>db.system.name</c> values to the Elastic APM subtype string expected by APM server.
		/// Only applied to ECS-mapped fields; raw OTel attributes are stored as-is.
		/// </summary>
		private static string NormalizeDbSystem(string dbSystem) =>
			// "azure.cosmosdb" is the stable OTel value; APM server / Kibana expect "cosmosdb" (matching the native integration).
			string.Equals(dbSystem, "azure.cosmosdb", StringComparison.OrdinalIgnoreCase)
				? ApiConstants.SubTypeCosmosDb
				: dbSystem;

		private static string ToResourceName(string type, string name) =>
			string.IsNullOrEmpty(name) ? type : $"{type}/{name}";
	}
}
#endif
