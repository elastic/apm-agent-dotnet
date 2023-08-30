// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	public ApmChannelOptions(HttpTransport transport) : base(transport) { }
}
