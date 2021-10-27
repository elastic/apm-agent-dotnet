// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Elastic.Apm.Api;

namespace OpenTelemetrySample
{
	public class OTSamples
	{
		public void Sample1()
		{
			var src = new ActivitySource("Test");

			using var activity1 = src.StartActivity(nameof(Sample1), ActivityKind.Server);

			Thread.Sleep(100);
			using var activity2 = src.StartActivity("foo");
			Thread.Sleep(150);
		}


		/// <summary>
		///  OTSpan
		///	 -           -
		///  ---> OTSpan ---> ElasticSpan
		/// </summary>
		/// <param name="tracer"></param>
		public void Sample2(ITracer tracer)
		{
			var src = new ActivitySource("Test");

			using (var activity1 = src.StartActivity(nameof(Sample2), ActivityKind.Server))
			{
				Thread.Sleep(100);
				using (var activity2 = src.StartActivity("foo")) Thread.Sleep(150);

				tracer.CurrentTransaction.CaptureSpan("ElasticApmSpan", "test", () => Thread.Sleep(50));
			}
		}

		/// <summary>
		/// OTSpan
		///      -
		///      ---> OTSpan
		///                -
		///                ---> ElasticSpan
		/// </summary>
		/// <param name="tracer"></param>
		public void Sample3(ITracer tracer)
		{
			var src = new ActivitySource("Test");

			using (var activity1 = src.StartActivity(nameof(Sample3), ActivityKind.Server))
			{
				Thread.Sleep(100);
				using (var activity2 = src.StartActivity("foo"))
				{
					tracer.CurrentSpan.CaptureSpan("ElasticApmSpan", "test", () => Thread.Sleep(50));
					Thread.Sleep(150);
				}
			}
		}

		/// <summary>
		/// ElasticTransaction
		///      -
		///      ---> OTSpan
		///                -
		///                ---> ElasticSpan
		/// </summary>
		/// <param name="tracer"></param>
		public void Sample4(ITracer tracer)
		{
			var src = new ActivitySource("Test");

			tracer.CaptureTransaction( nameof(Sample4), "test", t =>
			{
				Thread.Sleep(100);
				using (var activity = src.StartActivity("foo"))
				{

					tracer.CurrentSpan.CaptureSpan("ElasticApmSpan", "test", () => Thread.Sleep(50));
					Thread.Sleep(150);
				}
			});
		}

		public void OneSpanWithAttributes()
		{
			var src = new ActivitySource("Test");
			using (var activity = src.StartActivity("foo", ActivityKind.Server)) activity?.SetTag("foo", "bar");
		}

		public void TwoSpansWithAttributes()
		{
			var src = new ActivitySource("Test");
			using (var activity1 = src.StartActivity("foo", ActivityKind.Server))
			{
				activity1?.SetTag("foo1", "bar1");
				using (var activity2 = src.StartActivity("bar", ActivityKind.Internal)) activity2?.SetTag("foo2", "bar2");
			}
		}

		public void SpanKindSample()
		{
			var src = new ActivitySource("Test");
			using (var _ = src.StartActivity("SpanKindSample", ActivityKind.Server))
			{

				using (var activity = src.StartActivity("httpSpan", ActivityKind.Client)) activity?.SetTag("http.url", "http://foo.bar");
				using (var activity = src.StartActivity("dbSpan", ActivityKind.Client)) activity?.SetTag("db.system", "mysql");
				using (var activity = src.StartActivity("grpcSpan", ActivityKind.Client)) activity?.SetTag("rpc.system", "grpc");
				using (var activity = src.StartActivity("messagingSpan", ActivityKind.Client)) activity?.SetTag("messaging.system", "rabbitmq");
			}
		}
	}
}
