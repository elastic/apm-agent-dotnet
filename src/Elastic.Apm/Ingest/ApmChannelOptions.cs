// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Ingest.Transport;
using Elastic.Transport;

namespace Elastic.Apm.Ingest;

/// <summary>
/// Channel options for <see cref="ApmChannel"/>
/// </summary>
public class ApmChannelOptions : TransportChannelOptionsBase<IIntakeRoot, EventIntakeResponse, IntakeErrorItem>
{
	/// <inheritdoc cref="ApmChannelOptions"/>
	private ApmChannelOptions(HttpTransport transport) : base(transport) { }

	public ApmChannelOptions(Uri serverEndpoint, TransportClient transportClient = null)
		: this(new DefaultHttpTransport(new TransportConfiguration(new SingleNodePool(serverEndpoint), connection: transportClient!)))
	{

	}
}
