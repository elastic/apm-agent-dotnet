﻿// Licensed to Elasticsearch B.V under
// one or more agreements.
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
			_payloadItemSerializer = new PayloadItemSerializer();

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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			var labelName = "myLabel";
			ITransaction transaction = null;
			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				SetLabel(t, labelValue, labelName);
				t.Context.InternalLabels.Value.InnerDictionary[labelName].Value.Should().Be(labelValue);
				transaction = t;
			});

			var jsonString = _payloadItemSerializer.Serialize(transaction);
			jsonString.Should().Contain(GetAssertString(labelValue, labelName));
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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			var labelName = "myLabel";
			ISpan span = null;
			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.CaptureSpan("testSpan", "test", s =>
				{
					span = s;
					SetLabel(s, labelValue, labelName);
					s.Context.InternalLabels.Value.InnerDictionary[labelName].Value.Should().Be(labelValue);
				});
			});

			var jsonString = _payloadItemSerializer.Serialize(span);
			jsonString.Should().Contain(GetAssertString(labelValue, labelName));
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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			ITransaction transaction = null;
			ISpan span = null;
			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				transaction = t;
				t.CaptureSpan("testSpan", "test", s =>
				{
					span = s;
					SetLabel(s, 1, "intLabel");
					SetLabel(s, "abc", "stringLabel");
					SetLabel(s, true, "boolLabel");

					s.Context.InternalLabels.Value.InnerDictionary["intLabel"].Value.Should().Be(1);
					s.Context.InternalLabels.Value.InnerDictionary["stringLabel"].Value.Should().Be("abc");
					s.Context.InternalLabels.Value.InnerDictionary["boolLabel"].Value.Should().Be(true);
				});

				SetLabel(t, 1, "intLabel");
				SetLabel(t, "abc", "stringLabel");
				SetLabel(t, true, "boolLabel");

				t.Context.InternalLabels.Value.InnerDictionary["intLabel"].Value.Should().Be(1);
				t.Context.InternalLabels.Value.InnerDictionary["stringLabel"].Value.Should().Be("abc");
				t.Context.InternalLabels.Value.InnerDictionary["boolLabel"].Value.Should().Be(true);
			});

			var transactionJsonString = _payloadItemSerializer.Serialize(transaction);
			transactionJsonString.Should().Contain("\"intLabel\":1,\"stringLabel\":\"abc\",\"boolLabel\":true");

			var spanJsonString = _payloadItemSerializer.Serialize(span);
			spanJsonString.Should().Contain("\"intLabel\":1,\"stringLabel\":\"abc\",\"boolLabel\":true");
		}

		[Fact]
		public void LabelsOnErrorTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			ITransaction transaction = null;
			ISpan span = null;
			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				transaction = t;
				t.Labels["foo1"] = "bar1";
				t.Context.InternalLabels.Value.MergedDictionary["foo1"].Value.Should().Be("bar1");

				t.CaptureSpan("testSpan", "test", s =>
				{
					span = s;
					s.Labels["foo2"] = "bar2";
					s.Context.InternalLabels.Value.MergedDictionary["foo2"].Value.Should().Be("bar2");
				});
			});

			var transactionJsonString = _payloadItemSerializer.Serialize(transaction);
			transactionJsonString.Should().Contain(GetAssertString("bar1", "foo1"));

			var spanJsonString = _payloadItemSerializer.Serialize(span);
			spanJsonString.Should().Contain(GetAssertString("bar2", "foo2"));
		}

		[Fact]
		public void PublicStringDictionaryPropertyInSyncTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			var transaction = agent.Tracer.StartTransaction("test", "test");

			transaction.Labels["foo"] = "bar";
			transaction.SetLabel("intItem", 42);
			transaction.Labels.Remove("foo");

			transaction.End();

			var spanJsonString = _payloadItemSerializer.Serialize(transaction);
			spanJsonString.Should().Contain(GetAssertString(42, "intItem"));
			spanJsonString.Should().NotContain("foo");
			spanJsonString.Should().NotContain("bar");
		}

		[Fact]
		public void ReadLabels()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.SetLabel("fooT", 42);
				t.SetLabel("barT", false);

				t.CaptureSpan("test", "test", s =>
				{
					s.SetLabel("fooS", 43);
					s.SetLabel("barS", true);

					s.TryGetLabel<int>("fooS", out var fooSValue).Should().BeTrue();
					fooSValue.Should().Be(43);
					s.TryGetLabel<bool>("barS", out var barSValue).Should().BeTrue();
					barSValue.Should().BeTrue();
				});

				t.TryGetLabel<int>("fooT", out var fooTValue).Should().BeTrue();
				fooTValue.Should().Be(42);
				t.TryGetLabel<bool>("barT", out var barTValue).Should().BeTrue();
				barTValue.Should().BeFalse();
			});
		}

		/// <summary>
		/// Utilizes <see cref="IExecutionSegment.TryGetLabel{T}" /> with mismatched types.
		/// If the type parameter does not match the type of the given label, <see cref="IExecutionSegment.TryGetLabel{T}" />
		/// returns
		/// <code>default</code>.
		/// </summary>
		[Fact]
		public void ReadLabelGenericTypeTest()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("test", "test", t =>
			{
				t.SetLabel("intLabel", 42);
				t.SetLabel("boolLabel", true);
				t.SetLabel("stringLabel", "foo");

				t.TryGetLabel<string>("intLabel", out _).Should().BeFalse();
				t.TryGetLabel<bool>("intLabel", out _).Should().BeFalse();
				t.TryGetLabel<int>("intLabel", out var intVal).Should().BeTrue();
				intVal.Should().Be(42);

				t.TryGetLabel<string>("boolLabel", out _).Should().BeFalse();
				t.TryGetLabel<bool>("boolLabel", out var boolVal).Should().BeTrue();
				boolVal.Should().BeTrue();
				t.TryGetLabel<int>("boolLabel", out _).Should().BeFalse();

				t.TryGetLabel<string>("stringLabel", out var strVal).Should().BeTrue();
				strVal.Should().Be("foo");
				t.TryGetLabel<bool>("stringLabel", out _).Should().BeFalse();
				t.TryGetLabel<int>("stringLabel", out _).Should().BeFalse();
			});
		}

		[Fact]
		public void ReadLabelsWithMixedApiUsage()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			var t = agent.Tracer.StartTransaction("test", "test");

			t.SetLabel("foo", 42);
			t.SetLabel("bar", false);

			t.Labels["oldApi"] = "43";

			t.TryGetLabel<int>("foo", out var fooVal).Should().BeTrue();
			fooVal.Should().Be(42);
			t.TryGetLabel<bool>("bar", out var barVal).Should().BeTrue();
			barVal.Should().BeFalse();

			// values from the Labels dictionary aren't visible through the new API
			t.TryGetLabel<string>("oldApi", out _).Should().BeFalse();

			t.End();

			var spanJsonString = _payloadItemSerializer.Serialize(t);
			spanJsonString.Should().Contain("\"tags\":{\"foo\":42,\"bar\":false,\"oldApi\":\"43\"");
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
