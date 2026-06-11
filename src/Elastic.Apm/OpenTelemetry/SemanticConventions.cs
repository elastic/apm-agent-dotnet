namespace Elastic.Apm.OpenTelemetry;

internal static class SemanticConventions
{
	// DATABASE
	public const string DbSystem = "db.system";
	public const string DbSystemName = "db.system.name"; // newer OTel convention (replaces db.system)
	public const string DbName = "db.name";
	// db.namespace is the current OTel convention replacing db.name. The value is system-specific:
	// MySQL/MongoDB: the database name. PostgreSQL: "{database}|{schema}". SQL Server: "{instance}|{database}". Redis: the numeric db index.
	public const string DbNamespace = "db.namespace";
	public const string DbStatement = "db.statement";
	public const string DbQueryText = "db.query.text"; // newer OTel convention (replaces db.statement)

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

	// NETWORK (stable, current)
	public const string NetworkPeerAddress = "network.peer.address";
	public const string NetworkPeerPort = "network.peer.port";

	// NET (legacy)
	public const string NetPeerIp = "net.peer.ip";
	public const string NetPeerName = "net.peer.name";
	public const string NetPeerPort = "net.peer.port";

	// MESSAGING
	public const string MessagingSystem = "messaging.system";
	public const string MessagingDestination = "messaging.destination";

	// RPC SYSTEM
	public const string RpcSystem = "rpc.system";
	public const string RpcService = "rpc.service";

	// AZURE
	public const string AzNamespace = "az.namespace";
}
