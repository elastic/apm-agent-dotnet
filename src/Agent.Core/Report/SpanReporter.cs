using System;
using System.Collections.Generic;
using System.Text;
using Elastic.Agent.Core.Model.Payload;

namespace Elastic.Agent.Core.Report
{
    internal static class SpanReporter
    {
		public static List<Span> Spans { get; set; } = new List<Span>();
	}
}
