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
using Elastic.Apm.DiagnosticListeners;
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

		// Server side HTTP activities carry the scheme, method and path rather than a full URL, under either the current
		// or the older convention. Any one of them identifies an HTTP request.
		internal static readonly string[] HttpServerAttributeKeys =
			[SemanticConventions.UrlFull, SemanticConventions.HttpUrl, SemanticConventions.UrlScheme, SemanticConventions.HttpScheme,
				SemanticConventions.HttpRequestMethod, SemanticConventions.HttpMethod, SemanticConventions.HttpRoute];

		internal static readonly string[] HttpRequestMethodAttributeKeys =
			[SemanticConventions.HttpRequestMethod, SemanticConventions.HttpMethod];

		internal static readonly string[] HttpRequestPathAttributeKeys =
			[SemanticConventions.UrlPath, SemanticConventions.HttpTarget];

		// The current convention records the scheme as 'url.scheme', the older one as 'http.scheme'.
		internal static readonly string[] HttpSchemeAttributeKeys =
			[SemanticConventions.UrlScheme, SemanticConventions.HttpScheme];

		// The host which served an incoming request. The 'net.peer.*' keys must not be used here: on a server activity
		// they describe the client. 'http.host' (the Host header, which may carry the port) and 'http.server_name' are
		// the older conventions.
		internal static readonly string[] HttpServerHostAttributeKeys =
		[
			SemanticConventions.ServerAddress, SemanticConventions.HttpHost, SemanticConventions.HttpServerName,
			SemanticConventions.NetHostName
		];

		internal static readonly string[] HttpServerPortAttributeKeys =
			[SemanticConventions.ServerPort, SemanticConventions.NetHostPort];

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
			// The ASP.NET Core request activity is an HTTP request by definition; on .NET 8 and 9 hosting records no tags
			// on it at all, so the operation name is the only evidence.
			if (activity.Kind == ActivityKind.Server && (TryGetStringValue(activity, SemanticConventions.RpcSystem, out _)
					|| TryGetStringValue(activity, HttpServerAttributeKeys, out _)
					|| string.Equals(activity.OperationName, KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, StringComparison.Ordinal)))
				transaction.Type = ApiConstants.TypeRequest;
			else if (activity.Kind == ActivityKind.Consumer && TryGetStringValue(activity, SemanticConventions.MessagingSystem, out _))
				transaction.Type = ApiConstants.TypeMessaging;
			else
				transaction.Type = "unknown";
		}

		/// <summary>
		/// Builds a transaction name for a server side HTTP activity from its tags, in the shape the ASP.NET Core
		/// integration uses: the request method followed by the route template when one is recorded, otherwise the path.
		/// Returns false when the activity carries no request method, which is the case for the ASP.NET Core request
		/// activity on .NET 8 and 9 unless an instrumentation library adds tags to it.
		/// </summary>
		internal static bool TryGetHttpServerRequestName(Activity activity, out string name)
		{
			name = null;

			if (!TryGetRequestMethod(activity, out var method))
				return false;

			if (TryGetStringValue(activity, SemanticConventions.HttpRoute, out var route) && !string.IsNullOrEmpty(route))
			{
				name = $"{method} {route}";
			}
			else if (TryGetStringValue(activity, HttpRequestPathAttributeKeys, out var path) && !string.IsNullOrEmpty(path))
			{
				// The older 'http.target' carries the query string as well; the name should not.
				var queryStart = path.IndexOf('?');
				if (queryStart >= 0)
					path = path.Substring(0, queryStart);

				name = $"{method} {path}";
			}
			else
			{
				name = method;
			}

			return true;
		}

		/// <summary>
		/// Reads the request method under either convention, resolving the '_OTHER' placeholder the current one records
		/// for a non-standard method back to the real method it keeps alongside it.
		/// </summary>
		private static bool TryGetRequestMethod(Activity activity, out string method)
		{
			if (!TryGetStringValue(activity, HttpRequestMethodAttributeKeys, out method))
				return false;

			if (method == "_OTHER" && TryGetStringValue(activity, SemanticConventions.HttpRequestMethodOriginal, out var originalMethod))
				method = originalMethod;

			return true;
		}

		/// <summary>
		/// Fills <c>context.request</c> for a server side HTTP activity from its tags, under either the current or the
		/// older convention. APM server rebuilds the same fields for the transaction document itself from the OTel
		/// attributes, but an error captured while the request is handled copies the transaction's context as it stands
		/// at that moment and carries no attributes of its own, so without this it has no URL or method at all.
		/// Returns false when the activity records no method, which the intake requires, or no URL.
		/// </summary>
		internal static bool TryUpdateHttpRequestContext(Transaction transaction, Activity activity)
		{
			// The context of an unsampled transaction is never serialized.
			if (activity.Kind != ActivityKind.Server || !transaction.IsSampled)
				return false;

			if (!TryGetRequestMethod(activity, out var method))
				return false;

			// Already filled, either when the activity started or through the Elastic API, which always wins. Checked
			// before the URL is built so that the call when the activity stops costs nothing once it is.
			if (transaction.Context.Request != null)
				return false;

			var url = BuildRequestUrl(activity);
			if (url == null)
				return false;

			transaction.Context.Request = new Request(method, url);
			return true;
		}

		/// <summary>
		/// Builds the request URL from the activity's tags. The current convention records the path and the query string
		/// separately ('url.path' and 'url.query'), the older one records both in 'http.target'. Only client side
		/// instrumentation records a full URL, so for an incoming request the absolute URL has to be rebuilt from the
		/// scheme, the host and the path; when either of the first two is missing, or the target is not a path at all,
		/// the parts which are known are recorded on their own.
		/// </summary>
		private static Url BuildRequestUrl(Activity activity)
		{
			string path = null;
			string query = null;

			if (TryGetStringValue(activity, SemanticConventions.UrlPath, out var urlPath) && !string.IsNullOrEmpty(urlPath))
			{
				path = urlPath;
				if (TryGetStringValue(activity, SemanticConventions.UrlQuery, out var urlQuery) && !string.IsNullOrEmpty(urlQuery))
					query = urlQuery;
			}
			else if (TryGetStringValue(activity, SemanticConventions.HttpTarget, out var target) && !string.IsNullOrEmpty(target))
			{
				var queryStart = target.IndexOf('?');
				path = queryStart < 0 ? target : target.Substring(0, queryStart);
				query = queryStart < 0 ? null : target.Substring(queryStart + 1);
			}

			TryGetStringValue(activity, HttpSchemeAttributeKeys, out var scheme);

			if (!TryGetStringValue(activity, HttpAttributeKeys, out var full) || string.IsNullOrEmpty(full))
			{
				if (path == null)
					return null;

				// A target which is not an absolute path, '*' for an OPTIONS request for example, cannot be appended to
				// an authority.
				var authority = BuildRequestAuthority(activity);
				full = string.IsNullOrEmpty(scheme) || string.IsNullOrEmpty(authority) || path[0] != '/'
					? null
					: $"{scheme}://{authority}{path}{(query == null ? string.Empty : "?" + query)}";
			}

			if (full != null && Uri.TryCreate(full, UriKind.Absolute, out var uri))
			{
				var url = Url.FromUri(uri);
				if (url != null)
				{
					// The unparsed value, matching what the ASP.NET Core integration records when the raw target of the
					// request is unavailable.
					url.Raw = full;
					return url;
				}
			}

			return path == null
				? null
				: new Url
				{
					PathName = path,
					Search = query ?? string.Empty,
					Raw = query == null ? path : $"{path}?{query}",
					Protocol = UrlUtils.GetProtocolName(scheme)
				};
		}

		/// <summary>
		/// Builds the authority of the request URL. The current convention records the host and the port separately,
		/// while the older 'http.host' is the Host header, which carries the port itself.
		/// </summary>
		private static string BuildRequestAuthority(Activity activity)
		{
			if (!TryGetStringValue(activity, HttpServerHostAttributeKeys, out var host) || string.IsNullOrEmpty(host))
				return null;

			var lastColon = host.LastIndexOf(':');
			var closingBracket = host.LastIndexOf(']');

			// A colon after the closing bracket of an IPv6 literal, or the only colon in the value, separates the port.
			if (lastColon > closingBracket && (closingBracket >= 0 || lastColon == host.IndexOf(':')))
				return host;

			// An IPv6 literal has to be bracketed to form a valid authority.
			if (closingBracket < 0 && lastColon >= 0)
				host = $"[{host}]";

			return TryGetStringValue(activity, HttpServerPortAttributeKeys, out var port) && !string.IsNullOrEmpty(port)
				? $"{host}:{port}"
				: host;
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
				|| TryGetStringValue(activity, HttpSchemeAttributeKeys, out _))
			{
				var hasHttpHost = TryGetStringValue(activity, SemanticConventions.HttpHost, out var httpHost);
				var hasHttpScheme = TryGetStringValue(activity, HttpSchemeAttributeKeys, out var httpScheme);
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

			// The intake specification requires span.context.service.target to carry a type or a name. Emitting one with
			// neither, which happens for any activity whose attributes match none of the branches above, makes APM Server
			// reject the span. Bridged spans are not exit spans, so the inference in Span.End does not run for them and
			// leaving the target unset is the correct outcome.
			if (serviceTargetType != null || serviceTargetName != null)
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
