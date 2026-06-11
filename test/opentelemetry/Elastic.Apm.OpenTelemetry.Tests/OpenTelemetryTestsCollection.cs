// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Xunit;

namespace Elastic.Apm.OpenTelemetry.Tests;

// The ElasticActivityListener registers a global ActivityListener (ShouldListenTo = _ => true),
// so agents from parallel tests capture each other's activities. Grouping all OTel bridge tests
// into one collection forces sequential execution and prevents cross-test pollution.
[CollectionDefinition("OpenTelemetry")]
public class OpenTelemetryTestsCollection { }
