using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
			var mockPaylaodSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPaylaodSender));

			agent.Tracer.CaptureTransaction("test", "test", (t) =>
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
			var mockPaylaodSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPaylaodSender));

			agent.Tracer.CaptureTransaction("test", "test", (t) =>
			{
				t.CaptureSpan("testSpan", "test", (s) =>
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

		private void SetLabel(IExecutionSegment executionSegment, object labelValue, string labelName)
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

		private string GetAssertString(object labelValue, string labelName)
		{
			var serializedStrPattern = "\"tags\":{\"" + labelName + "\":";
			if (labelValue is string)
				serializedStrPattern += "\"";

			if (labelValue is bool boolVal)
				serializedStrPattern += boolVal.ToString().ToString().ToLower();
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
