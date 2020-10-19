// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LabelTests
	{
		[InlineData("StrValue")]
		[InlineData(123)]
		[InlineData(234.0)]
		[InlineData(346L)]
		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void SingleLabelOnTransactionTests(object labelValue)
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				var labelName = "myLabel";
				SetLabel(t, labelValue, labelName);
				var jsonString = JsonConvert.SerializeObject(t);
				jsonString.Should().Contain(GetAssertString(labelValue, labelName));

				t.Labels[labelName].Should().Be(labelValue);
				t.Context.Labels[labelName].Should().Be(labelValue);
			});
		}

		[InlineData("StrValue")]
		[InlineData(123)]
		[InlineData(234.0)]
		[InlineData(346L)]
		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void SingleLabelOnSpanTests(object labelValue)
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.CaptureSpan("testSpan", "test", s =>
				{
					var labelName = "myLabel";
					SetLabel(s, labelValue, labelName);
					var jsonString = JsonConvert.SerializeObject(s);
					jsonString.Should().Contain(GetAssertString(labelValue, labelName));

					s.Labels[labelName].Should().Be(labelValue);
					s.Context.Labels[labelName].Should().Be(labelValue);
				});
			});
		}

		[Theory]
		[InlineData("457")]
		public void SingleLabelOnTransactionTestsDec(string number)
		{
			var decimalValue = Convert.ToDecimal(number);
			SingleLabelOnTransactionTests(decimalValue);
		}

		[Theory]
		[InlineData("457")]
		public void SingleLabelOnSpanTestsDec(string number)
		{
			var decimalValue = Convert.ToDecimal(number);
			SingleLabelOnSpanTests(decimalValue);
		}

		[Fact]
		public void MultipleLabelsTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.CaptureSpan("testSpan", "test", s =>
				{
					SetLabel(s, 1, "intLabel");
					SetLabel(s, "abc", "stringLabel");
					SetLabel(s, true, "boolLabel");

					var spanJsonString = JsonConvert.SerializeObject(s);
					spanJsonString.Should().Contain("\"intLabel\":1,\"stringLabel\":\"abc\",\"boolLabel\":true");

					s.Labels["intLabel"].Should().Be(1);
					s.Context.Labels["intLabel"].Should().Be(1);

					s.Labels["stringLabel"].Should().Be("abc");
					s.Context.Labels["stringLabel"].Should().Be("abc");

					s.Labels["boolLabel"].Should().Be(true);
					s.Context.Labels["boolLabel"].Should().Be(true);
				});

				SetLabel(t, 1, "intLabel");
				SetLabel(t, "abc", "stringLabel");
				SetLabel(t, true, "boolLabel");

				var transactionJsonString = JsonConvert.SerializeObject(t);
				transactionJsonString.Should().Contain("\"intLabel\":1,\"stringLabel\":\"abc\",\"boolLabel\":true");

				t.Labels["intLabel"].Should().Be(1);
				t.Context.Labels["intLabel"].Should().Be(1);

				t.Labels["stringLabel"].Should().Be("abc");
				t.Context.Labels["stringLabel"].Should().Be("abc");

				t.Labels["boolLabel"].Should().Be(true);
				t.Context.Labels["boolLabel"].Should().Be(true);
			});
		}

		[Fact]
		public void LabelsOnErrorTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.SetLabel("intLabel", 5);
				t.CaptureError("Test Error", "TestMethod", new StackTrace().GetFrames(),
					labels: new Dictionary<string, object> { { "stringLabel", "test" } });

				// add label to transaction after the error - error does not contain this
				t.SetLabel("boolLabel", true);

				mockPayloadSender.FirstError.Context.Labels["intLabel"].Should().Be(5);
				mockPayloadSender.FirstError.Context.Labels["stringLabel"].Should().Be("test");
				mockPayloadSender.FirstError.Context.Labels.Should().NotContainKey("boolLabel");
			});
		}

		private static void SetLabel(IExecutionSegment executionSegment, object labelValue, string labelName)
		{
			switch (labelValue)
			{
				case string strVal:
					executionSegment.SetLabel(labelName, strVal);
					break;
				case int intVal:
					executionSegment.SetLabel(labelName, intVal);
					break;
				case double doubleVal:
					executionSegment.SetLabel(labelName, doubleVal);
					break;
				case long longVal:
					executionSegment.SetLabel(labelName, longVal);
					break;
				case decimal decimalValue:
					executionSegment.SetLabel(labelName, decimalValue);
					break;
				case bool boolValue:
					executionSegment.SetLabel(labelName, boolValue);
					break;
				default:
					throw new Exception("Unexpected Type");
			}
		}

		private static string GetAssertString(object labelValue, string labelName)
		{
			var serializedStrPattern = "\"tags\":{\"" + labelName + "\":";
			if (labelValue is string)
				serializedStrPattern += "\"";

			if (labelValue is bool boolVal)
				serializedStrPattern += boolVal.ToString().ToLower();
			else if (labelValue is double doubleValToStr)
				serializedStrPattern += string.Format(CultureInfo.InvariantCulture, "{0:N1}", doubleValToStr);
			else if (labelValue is decimal dedimalValToStr)
				serializedStrPattern += string.Format(CultureInfo.InvariantCulture, "{0:N1}", dedimalValToStr);
			else
				serializedStrPattern += labelValue;

			if (labelValue is string)
				serializedStrPattern += "\"";
			serializedStrPattern += "}";

			return serializedStrPattern;
		}
	}
}
