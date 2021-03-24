// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.DistributedTracing;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.DistributedTracing
{
	public class TraceStateTests
	{
		private readonly TraceState _traceState;

		public TraceStateTests() => _traceState = new TraceState();

		[Fact]
		public void New_TraceState_Should_Have_Null_Header_And_SampleRate()
		{
			_traceState.ToTextHeader().Should().BeNull();
			_traceState.SampleRate.Should().BeNull();
		}

		[Theory]
		[InlineData(0,"one=two", null)]
		[InlineData(7,"one=two", "one=two")]
		[InlineData(20,"one=two_three=four", "one=two,three=four")] // no overflow
		[InlineData(20,"one=two_three=four_five=six_seven=eight", "one=two,three=four")] // overflow after 'four'
		[InlineData(20,"one=two,three=four_five=six,seven=eight", "one=two,three=four")] // overflow within 2cnd header
		[InlineData(20,"one=two_three=four_five=six,seven=eight", "one=two,three=four")] // overflow within first header
		[InlineData(20,"one=two_three=four,X,five=six_seven=eight,nine=ten", "one=two,three=four,X")] // empty entry kept as-is
		[InlineData(20,"one=two_three=four,five=six,seven=eight_nine=ten", "one=two,three=four")] // multiple overflow values
		[InlineData(18,"one=two_three=four,five=six,seven=eight_nine=ten", "one=two,three=four")] // cutoff on separator
		[InlineData(17,"one=two_three=four,five=six,seven=eight_nine=ten,eleven-twelve", "one=two,nine=ten")] // just fits
		public void Header_Should_Be_Limited_By_SizeLimit(int limit, string headers, string expected)
		{
			static string ReplaceSpaces(string s) => s?.Replace('X', ' ');

			_traceState.SizeLimit = limit;

			foreach (var h in headers.Split('_'))
				_traceState.AddTextHeader(ReplaceSpaces(h));

			_traceState.ToTextHeader().Should().Be(ReplaceSpaces(expected));

			// none of those values should have a sample rate set
			_traceState.SampleRate.Should().BeNull();
		}

		[Fact]
		public void Added_Sample_Rate_Should_Be_Set_On_TraceState()
		{
			var sampleRate = 0.5;
			var headerValue = TraceState.GetHeaderValue(sampleRate);
			headerValue.Should().Be("es=s:0.5");

			var traceState = new TraceState(sampleRate);
			traceState.SampleRate.Should().Be(sampleRate);
			traceState.ToTextHeader().Should().Be(headerValue);
		}

		[Fact]
		public void Multiple_Vendors_In_One_Header_Should_Be_Present_In_TextHeader()
		{
			_traceState.AddTextHeader("aa=1|2|3,es=s:0.5,bb=4|5|6");
			_traceState.SampleRate.Should().Be(0.5d);
			_traceState.ToTextHeader().Should().Be("aa=1|2|3,es=s:0.5,bb=4|5|6");
		}

		[Fact]
		public void Other_Vendors_In_Header_Should_Be_Present_In_TextHeader()
		{
			_traceState.AddTextHeader("aa=1|2|3");
			_traceState.AddTextHeader("bb=4|5|6");
			_traceState.ToTextHeader().Should().Be("aa=1|2|3,bb=4|5|6");
			_traceState.SampleRate.Should().BeNull();
		}

		[Fact]
		public void SampleRate_Should_Be_Set_From_Headers()
		{
			_traceState.AddTextHeader("aa=1|2|3");
			_traceState.AddTextHeader("es=s:0.5");
			_traceState.SampleRate.Should().Be(0.5d);
			_traceState.AddTextHeader("bb=4|5|6");
			_traceState.ToTextHeader().Should().Be("aa=1|2|3,es=s:0.5,bb=4|5|6");
		}

		[Fact]
		public void SampleRateAddedToHeaders()
		{
			_traceState.AddTextHeader("aa=1|2|3");
			_traceState.AddTextHeader("bb=4|5|6");

			_traceState.SetSampleRate(0.444);
			_traceState.SampleRate.Should().Be(0.444);
			_traceState.ToTextHeader().Should().Be("es=s:0.444,aa=1|2|3,bb=4|5|6");
		}

		[Theory]
		[InlineData("es=s:0.5", 0.55554, "es=s:0.5555")]
		[InlineData("aa=1;2,es=s:0.5", 0.444, "es=s:0.444,aa=1;2")]
		[InlineData("aa=1;2,es=s:0.5,bb=4|5|6,", 0.444, "es=s:0.444,aa=1;2,bb=4|5|6,")]
		[InlineData("aa=1;2,bb=4|5|6,", 0.55554, "es=s:0.5555,aa=1;2,bb=4|5|6,")]
		[InlineData("es=f:1,aa=1;2,bb=4|5|6,", 0.55554, "es=f:1;s:0.5555,aa=1;2,bb=4|5|6,")]
		[InlineData("aa=1;2,es=f:1,bb=4|5|6,", 0.55554, "es=f:1;s:0.5555,aa=1;2,bb=4|5|6,")]
		public void SampleRate_Should_Mutate_ElasticVendor_Set_From_Header(string header, double sampleRate, string expected)
		{
			_traceState.AddTextHeader(header);
			_traceState.SetSampleRate(sampleRate);
			_traceState.SampleRate.Should().Be(sampleRate);
			_traceState.ToTextHeader().Should().Be(expected);
		}

		[Theory]
		[InlineData("es=k:0;s:0.555555,aa=123", "es=k:0;s:0.5556,aa=123")]
		[InlineData("es=s:0.555555;k:0,aa=123", "es=s:0.5556;k:0,aa=123")]
		public void Unknown_Keys_Should_Be_Ignored(string header, string rewrittenHeader)
		{
			_traceState.AddTextHeader(header);
			_traceState.SampleRate.Should().Be(0.5556);
			_traceState.ToTextHeader().Should().Be(rewrittenHeader);
		}

		[Theory]
		[InlineData("es=")]
		[InlineData("es=s:")]
		[InlineData("es=s:-1")]
		[InlineData("es=s:2")]
		[InlineData("es=s:aa")]
		public void Invalid_Values_Should_Be_Ignored(string header)
		{
			_traceState.AddTextHeader(header);
			_traceState.SampleRate.Should().BeNull();
		}

		[Theory]
		[InlineData("0.55554", 0.5555)]
		[InlineData("0.55555", 0.5556)]
		[InlineData("0.55556", 0.5556)]
		public void Rounding_Should_Be_Applied_ToUpstream_Header(string headerRate, double expectedRate)
		{
			_traceState.AddTextHeader("es=s:" + headerRate);
			_traceState.SampleRate.Should().Be(expectedRate);
			_traceState.ToTextHeader().Should().Be("es=s:" + expectedRate);
		}
	}
}
