// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET || NETSTANDARD2_1
using System;
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
			[SemanticConventions.ServerPort, SemanticConventions.NetPeerPort];

		internal static readonly string[] ServerAddressAttributeKeys =
			[SemanticConventions.ServerAddress, SemanticConventions.NetPeerName, SemanticConventions.NetPeerIp];

		// Canonical URL/host-presence keys used both as "is this HTTP?" and for URL parsing.
		internal static readonly string[] HttpAttributeKeys =
			[SemanticConventions.UrlFull, SemanticConventions.HttpUrl];

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
			var netName = string.Empty;

			if (TryGetStringValue(activity, ServerPortAttributeKeys, out var netPortValue))
				peerPort = netPortValue;

			if (TryGetStringValue(activity, ServerAddressAttributeKeys, out var netNameValue))
				netName = netNameValue;

			if (netName.Length > 0 && peerPort.Length > 0)
			{
				netName += ':';
				netName += peerPort;
			}

			string serviceTargetType = null;
			string serviceTargetName = null;
			string resource = null;

			if (TryGetStringValue(activity, SemanticConventions.DbSystem, out var dbSystem))
			{
				span.Type = ApiConstants.TypeDb;
				span.Subtype = dbSystem;
				serviceTargetType = span.Subtype;
				serviceTargetName = TryGetStringValue(activity, SemanticConventions.DbName, out var dbName) ? dbName : null;
				resource = ToResourceName(span.Subtype, serviceTargetName);
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

		private static string ToResourceName(string type, string name) =>
			string.IsNullOrEmpty(name) ? type : $"{type}/{name}";
	}
}
#endif
