namespace Elastic.Apm.OpenTelemetry;

internal static class SemanticConventions
{
	// DATABASE
	public const string DbSystem = "db.system";
	public const string DbName = "db.name";

	// HTTP
	public const string HttpUrl = "http.url";
	public const string HttpStatusCode = "http.status_code";
	public const string HttpMethod = "http.method";
	public const string HttpScheme = "http.scheme";
	public const string HttpHost = "http.host";

	// HTTP REQUEST
	public const string HttpRequestMethod = "http.request.method";
	public const string HttpRequestBodySize = "http.request.body.size";

	// HTTP RESPONSE
	public const string HttpResponseStatusCode = "http.response.status_code";
	public const string HttpResponseBodySize = "http.response.body.size";

	// URL
	public const string UrlFull = "url.full";

	// SERVER
	public const string ServerAddress = "server.address";
	public const string ServerPort = "server.port";

	// NET
	public const string NetPeerIp = "net.peer.name";
	public const string NetPeerName = "net.peer.name";
	public const string NetPeerPort = "net.peer.port";

	// MESSAGING
	public const string MessagingSystem = "messaging.system";
	public const string MessagingDestination = "messaging.destination";

	// RPC SYSTEM
	public const string RpcSystem = "rpc.system";
	public const string RpcService = "rpc.service";
}
