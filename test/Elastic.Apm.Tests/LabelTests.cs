// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LabelTests
	{
		private readonly PayloadItemSerializer _payloadItemSerializer;

		public LabelTests() =>
			_payloadItemSerializer = new PayloadItemSerializer(new MockConfigSnapshot());

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

				t.Context.InternalLabels.Value.InnerDictionary[labelName].Value.Should().Be(labelValue);

				var jsonString = SerializePayloadItem(t);
				jsonString.Should().Contain(GetAssertString(labelValue, labelName));
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
					var jsonString = new PayloadItemSerializer(new MockConfigSnapshot()).SerializeObject(s);
					jsonString.Should().Contain(GetAssertString(labelValue, labelName));

					s.Context.InternalLabels.Value.InnerDictionary[labelName].Value.Should().Be(labelValue);
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

					var spanJsonString = SerializePayloadItem(s);
					spanJsonString.Should().Contain("\"intLabel\":1,\"stringLabel\":\"abc\",\"boolLabel\":true");

					s.Context.InternalLabels.Value.InnerDictionary["intLabel"].Value.Should().Be(1);
					s.Context.InternalLabels.Value.InnerDictionary["stringLabel"].Value.Should().Be("abc");
					s.Context.InternalLabels.Value.InnerDictionary["boolLabel"].Value.Should().Be(true);
				});

				SetLabel(t, 1, "intLabel");
				SetLabel(t, "abc", "stringLabel");
				SetLabel(t, true, "boolLabel");

				var transactionJsonString = SerializePayloadItem(t);
				transactionJsonString.Should().Contain("\"intLabel\":1,\"stringLabel\":\"abc\",\"boolLabel\":true");

				t.Context.InternalLabels.Value.InnerDictionary["intLabel"].Value.Should().Be(1);
				t.Context.InternalLabels.Value.InnerDictionary["stringLabel"].Value.Should().Be("abc");
				t.Context.InternalLabels.Value.InnerDictionary["boolLabel"].Value.Should().Be(true);
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
					labels: new Dictionary<string, Label> { { "stringLabel", "test" } });

				// add label to transaction after the error - error does not contain this
				t.SetLabel("boolLabel", true);

				mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary["intLabel"].Value.Should().Be(5);
				mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary["stringLabel"].Value.Should().Be("test");
				mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary.Should().NotContainKey("boolLabel");
			});
		}

		[Fact]
		public void SameLabelTwice()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.SetLabel("intLabel", 5);
				t.SetLabel("intLabel", 6);
			});

			mockPayloadSender.FirstTransaction.Context.InternalLabels.Value.InnerDictionary["intLabel"].Value.Should().Be(6);
		}
#pragma warning disable CS0618 // Type or member is obsolete
		//For testing backwards compatibility we also test the obsolete Dictionary<string,string> Labels property here.

		[Fact]
		public void PublicStringDictionaryPropertySerializationTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.Labels["foo1"] = "bar1";
				var transactionJsonString = SerializePayloadItem(t);
				transactionJsonString.Should().Contain(GetAssertString("bar1", "foo1"));
				t.Context.InternalLabels.Value.MergedDictionary["foo1"].Value.Should().Be("bar1");


				t.CaptureSpan("testSpan", "test", s =>
				{
					s.Labels["foo2"] = "bar2";
					var spanJsonString = SerializePayloadItem(s);
					spanJsonString.Should().Contain(GetAssertString("bar2", "foo2"));

					s.Context.InternalLabels.Value.MergedDictionary["foo2"].Value.Should().Be("bar2");
				});
			});
		}

		[Fact]
		public void PublicStringDictionaryPropertyInSyncTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			var transaction = agent.Tracer.StartTransaction("test", "test");

			transaction.Labels["foo"] = "bar";

			transaction.Labels.Should().HaveCount(1);
			transaction.Labels.Keys.Should().HaveCount(1);

			transaction.Labels.Keys.First().Should().Be("foo");
			transaction.Context.InternalLabels.Value.MergedDictionary["foo"].Value.Should().Be("bar");

			transaction.SetLabel("item2", 123);

			//assert that the string API still only contains 1 item:
			transaction.Labels.Should().HaveCount(1);
			transaction.Context.InternalLabels.Value.MergedDictionary.Keys.Should().HaveCount(2);

			foreach (var item in transaction.Labels)
			{
				item.Key.Should().Be("foo");
				item.Value.Should().Be("bar");
			}

			transaction.Labels.Clear();
			transaction.Labels.Keys.Should().HaveCount(0);

			transaction.End();
		}

		[Fact]
		public void PublicStringDictionaryPropertyRemoveItem()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			var transaction = agent.Tracer.StartTransaction("test", "test");

			transaction.Labels["foo"] = "bar";
			transaction.SetLabel("intItem", 42);
			transaction.Labels.Remove("foo");

			var spanJsonString = SerializePayloadItem(transaction);
			spanJsonString.Should().Contain(GetAssertString(42, "intItem"));
			spanJsonString.Should().NotContain("foo");
			spanJsonString.Should().NotContain("bar");

			transaction.End();
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

		private string SerializePayloadItem(object item) =>
			_payloadItemSerializer.SerializeObject(item);
	}
}
