// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Contains tests related to json serialization.
	/// </summary>
	public class SerializationTests
	{
		private readonly PayloadItemSerializer _payloadItemSerializer;

		public SerializationTests() =>
			_payloadItemSerializer = new PayloadItemSerializer(new MockConfigSnapshot());

		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData SerializationUtilsTrimToPropertyMaxLengthVariantsToTest => new TheoryData<string, string>
		{
			{ "", "" },
			{ "A", "A" },
			{ "B".Repeat(Consts.PropertyMaxLength), "B".Repeat(Consts.PropertyMaxLength) },
			{ "C".Repeat(Consts.PropertyMaxLength + 1), "C".Repeat(Consts.PropertyMaxLength - 3) + "..." },
			{ "D".Repeat(Consts.PropertyMaxLength * 2), "D".Repeat(Consts.PropertyMaxLength - 3) + "..." }
		};

		/// <summary>
		/// Tests the <see cref="TruncateJsonConverter" />. It serializes a transaction with Transaction.Name.Length > 1024.
		/// Makes sure that the Transaction.Name is truncated correctly.
		/// </summary>
		[Fact]
		public void StringTruncationTest()
		{
			var str = new string('a', 1200);

			string json;
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				var transaction = new Transaction(agent, str, "test") { Duration = 1, Result = "fail" };
				json = SerializePayloadItem(transaction);
			}

			var deserializedTransaction = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedTransaction);

			Assert.Equal(Consts.PropertyMaxLength, deserializedTransaction["name"]?.Value<string>()?.Length);
			Assert.Equal("...", deserializedTransaction["name"].Value<string>().Substring(1021, 3));
			Assert.Equal("test", deserializedTransaction["type"].Value<string>());
			Assert.Equal("fail", deserializedTransaction["result"].Value<string>());
		}

		/// <summary>
		/// It creates an instance of <see cref="DummyType" /> with a <see cref="DummyType.StringProp" /> that has a string
		/// which is longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure that the serialized instance still contains the whole string (aka it was not trimmed), since
		/// the property pointing to the string wasn't marked with any attributes, so it's not trimmed.
		/// </summary>
		[Fact]
		public void StringNoTruncateAttributeTest()
		{
			var str = new string('a', 1200);

			var dummyInstance = new DummyType { IntProp = 42, StringProp = str };

			var json = SerializePayloadItem(dummyInstance);
			var deserializedDummyInstance = JsonConvert.DeserializeObject<DummyType>(json);

			Assert.NotNull(deserializedDummyInstance);

			Assert.Equal(str.Length, deserializedDummyInstance.StringProp.Length);
			Assert.Equal(str, deserializedDummyInstance.StringProp);
			Assert.Equal(42, deserializedDummyInstance.IntProp);
		}

		/// <summary>
		/// Creates a <see cref="Context" /> that has <see cref="Context.Labels" />
		/// with strings longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure that the long string is trimmed.
		/// </summary>
		[Fact]
		public void LabelsTruncation()
		{
			var str = new string('a', 1200);

			var context = new Context();
			context.Labels["foo"] = str;

			var json = SerializePayloadItem(context);
			var deserializedContext = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedContext);

			// In Intake API the property is still named `tags'
			// See https://github.com/elastic/apm-server/blob/6.5/docs/spec/context.json#L43
			const string intakeApiLabelsPropertyName = "tags";
			Assert.Equal(Consts.PropertyMaxLength, deserializedContext[intakeApiLabelsPropertyName]["foo"].Value<string>().Length);
			Assert.Equal("...", deserializedContext[intakeApiLabelsPropertyName]["foo"].Value<string>().Substring(1021, 3));
		}

		/// <summary>
		/// Creates a <see cref="SpanContext" /> that has <see cref="SpanContext.Labels" />
		/// with strings longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure that the long string is trimmed.
		/// </summary>
		[Fact]
		public void LabelsTruncationSpanContext()
		{
			var str = new string('a', 1200);

			var context = new SpanContext();
			context.Labels["foo"] = str;

			var json = SerializePayloadItem(context);
			var deserializedContext = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedContext);

			// In Intake API the property is still named `tags'
			// See https://github.com/elastic/apm-server/blob/6.5/docs/spec/spans/common_span.json#L50
			const string intakeApiLabelsPropertyName = "tags";
			Assert.Equal(Consts.PropertyMaxLength, deserializedContext[intakeApiLabelsPropertyName]["foo"].Value<string>().Length);
			Assert.Equal("...", deserializedContext[intakeApiLabelsPropertyName]["foo"].Value<string>().Substring(1021, 3));
		}

		/// <summary>
		/// Makes sure that labels with `null` don't cause exception during serialization
		/// </summary>
		[Fact]
		public void LabelWithNullValueShouldBeCaptured()
		{
			var context = new SpanContext();
			context.Labels["foo"] = null;

			var json = SerializePayloadItem(context);
			var deserializedContext = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedContext);
			deserializedContext["tags"]["foo"].Value<string>().Should().BeNull();
		}

		/// <summary>
		/// Makes sure that labels with an empty string are captured and not causing any trouble
		/// </summary>
		[Fact]
		public void LabelWithEmptyStringShouldBeCaptured()
		{
			var context = new SpanContext();
			context.Labels["foo"] = string.Empty;

			var json = SerializePayloadItem(context);
			var deserializedContext = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedContext);
			deserializedContext["tags"]["foo"].Value<string>().Should().BeEmpty();
		}

		/// <summary>
		/// It creates an instance of <see cref="DummyType" /> with a <see cref="DummyType.DictionaryProp" /> that has a value
		/// which is longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure that the serialized instance still contains the whole value (aka it was not trimmed), since
		/// the property pointing to the string is not marked with any attributes, so it is not trimmed.
		/// </summary>
		[Fact]
		public void DictionaryNoTruncateAttributeTest()
		{
			var str = new string('a', 1200);

			var dummyInstance = new DummyType();
			dummyInstance.DictionaryProp["foo"] = str;

			var json = SerializePayloadItem(dummyInstance);
			var deserializedDummyInstance = JsonConvert.DeserializeObject<JObject>(json);

			Assert.NotNull(deserializedDummyInstance);

			Assert.Equal(str.Length, deserializedDummyInstance["dictionaryProp"]["foo"].Value<string>().Length);
			Assert.Equal(str, deserializedDummyInstance["dictionaryProp"]["foo"].Value<string>());
		}

		/// <summary>
		/// Creates a db instance with a statement that is longer than 10 000 characters.
		/// Makes sure the statement is truncated.
		/// </summary>
		[Fact]
		public void DbStatementLengthTest()
		{
			var str = new string('a', 12000);
			var db = new Database { Statement = str };

			var json = SerializePayloadItem(db);
			var deserializedDb = JsonConvert.DeserializeObject<Database>(json);

			Assert.NotNull(deserializedDb);

			Assert.Equal(10_000, deserializedDb.Statement.Length);
			Assert.Equal("...", deserializedDb.Statement.Substring(10_000 - 3, 3));
		}

		[Fact]
		public void ServiceVersionLengthTest()
		{
			var logger = new NoopLogger();
			var service = Service.GetDefaultService(new MockConfigSnapshot(logger), logger);
			service.Version = new string('a', 1200);

			var json = SerializePayloadItem(service);
			var deserializedService = JsonConvert.DeserializeObject<Service>(json);

			Assert.NotNull(deserializedService);

			Assert.Equal(Consts.PropertyMaxLength, deserializedService.Version.Length);
			Assert.Equal("...", deserializedService.Version.Substring(Consts.PropertyMaxLength - 3, 3));
		}

		/// <summary>
		/// Creates a service instance with a name that is longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure the name is truncated.
		/// </summary>
		[Fact]
		public void ServiceNameLengthTest()
		{
			var logger = new NoopLogger();
			var service = Service.GetDefaultService(new MockConfigSnapshot(logger), logger);
			service.Name = new string('a', 1200);

			var json = SerializePayloadItem(service);
			var deserializedService = JsonConvert.DeserializeObject<Service>(json);

			Assert.NotNull(deserializedService);

			Assert.Equal(Consts.PropertyMaxLength, deserializedService.Name.Length);
			Assert.Equal("...", deserializedService.Name.Substring(Consts.PropertyMaxLength - 3, 3));
		}

		/// <summary>
		/// Verifies that <see cref="Transaction.Context" /> is serialized only when the transaction is sampled.
		/// </summary>
		[Fact]
		public void TransactionContextShouldBeSerializedOnlyWhenSampled()
		{
			var agent = new TestAgentComponents();
			// Create a transaction that is sampled (because the sampler is constant sampling-everything sampler
			var sampledTransaction = new Transaction(agent.Logger, "dummy_name", "dumm_type", new Sampler(1.0), /* distributedTracingData: */ null,
				agent.PayloadSender, new MockConfigSnapshot(new NoopLogger()), agent.TracerInternal.CurrentExecutionSegmentsContainer);
			sampledTransaction.Context.Request = new Request("GET",
				new Url { Full = "https://elastic.co", Raw = "https://elastic.co", HostName = "elastic.co", Protocol = "HTTP" });

			// Create a transaction that is not sampled (because the sampler is constant not-sampling-anything sampler
			var nonSampledTransaction = new Transaction(agent.Logger, "dummy_name", "dumm_type", new Sampler(0.0), /* distributedTracingData: */ null,
				agent.PayloadSender, new MockConfigSnapshot(new NoopLogger()), agent.TracerInternal.CurrentExecutionSegmentsContainer);
			nonSampledTransaction.Context.Request = sampledTransaction.Context.Request;

			var serializedSampledTransaction = SerializePayloadItem(sampledTransaction);
			var deserializedSampledTransaction = JsonConvert.DeserializeObject<JObject>(serializedSampledTransaction);
			var serializedNonSampledTransaction = SerializePayloadItem(nonSampledTransaction);
			var deserializedNonSampledTransaction = JsonConvert.DeserializeObject<JObject>(serializedNonSampledTransaction);

			// ReSharper disable once PossibleNullReferenceException
			deserializedSampledTransaction["sampled"].Value<bool>().Should().BeTrue();
			deserializedSampledTransaction["context"]["request"]["url"]["full"]
				.Value<string>()
				.Should()
				.Be("https://elastic.co");

			// ReSharper disable once PossibleNullReferenceException
			deserializedNonSampledTransaction["sampled"].Value<bool>().Should().BeFalse();
			deserializedNonSampledTransaction.Should().NotContainKey("context");
		}

		[Theory]
		[InlineData("", 0, "")]
		[InlineData("A", 0, "")]
		[InlineData("ABC", 0, "")]
		[InlineData("B", 1, "B")]
		[InlineData("", 1, "")]
		[InlineData("ABC", 1, "A")]
		[InlineData("ABC", 2, "AB")]
		[InlineData("ABC", 3, "ABC")]
		[InlineData("", 3, "")]
		[InlineData("ABCE", 3, "ABC")]
		[InlineData("ABCD", 4, "ABCD")]
		[InlineData("ABCDE", 4, "ABCD")]
		[InlineData("ABCDE", 5, "ABCDE")]
		[InlineData("ABCDEF", 5, "ABCDE")]
		[InlineData("ABCDEF", 6, "ABCDEF")]
		[InlineData("ABCDEFG", 6, "ABC...")]
		[InlineData("ABCDEFGH", 6, "ABC...")]
		[InlineData("ABCDEFGH", 7, "ABCD...")]
		public void SerializationUtilsTruncateTests(string original, int maxLength, string expectedTrimmed) =>
			original.Truncate(maxLength).Should().Be(expectedTrimmed);

		[Theory]
		[MemberData(nameof(SerializationUtilsTrimToPropertyMaxLengthVariantsToTest))]
		public void SerializationUtilsTruncateToPropertyMaxLengthTests(string original, string expectedTrimmed)
		{
			Consts.PropertyMaxLength.Should().BeGreaterThan(3);
			original.Truncate().Should().Be(expectedTrimmed);
		}

		/// <summary>
		/// Makes sure that keys in the label are de dotted.
		/// </summary>
		[Fact]
		public void LabelDeDotting()
		{
			var context = new Context();
			context.Labels["a.b"] = "labelValue";
			var json = SerializePayloadItem(context);
			json.Should().Be("{\"tags\":{\"a_b\":\"labelValue\"}}");

			context = new Context();
			context.Labels["a.b.c"] = "labelValue";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"tags\":{\"a_b_c\":\"labelValue\"}}");

			context = new Context();
			context.Labels["a.b"] = "labelValue1";
			context.Labels["a.b.c"] = "labelValue2";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"tags\":{\"a_b\":\"labelValue1\",\"a_b_c\":\"labelValue2\"}}");

			context = new Context();
			context.Labels["a\"b"] = "labelValue";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"tags\":{\"a_b\":\"labelValue\"}}");

			context = new Context();
			context.Labels["a*b"] = "labelValue";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"tags\":{\"a_b\":\"labelValue\"}}");

			context = new Context();
			context.Labels["a*b"] = "labelValue1";
			context.Labels["a\"b_c"] = "labelValue2";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"tags\":{\"a_b\":\"labelValue1\",\"a_b_c\":\"labelValue2\"}}");
		}

    /// <summary>
		/// Makes sure that keys in custom are de dotted.
		/// </summary>
		[Fact]
		public void CustomDeDotting()
		{
			var context = new Context();
			context.Custom["a.b"] = "customValue";
			var json = SerializePayloadItem(context);
			json.Should().Be("{\"custom\":{\"a_b\":\"customValue\"}}");

			context = new Context();
			context.Custom["a.b.c"] = "customValue";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"custom\":{\"a_b_c\":\"customValue\"}}");

			context = new Context();
			context.Custom["a.b"] = "customValue1";
			context.Custom["a.b.c"] = "customValue2";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"custom\":{\"a_b\":\"customValue1\",\"a_b_c\":\"customValue2\"}}");

			context = new Context();
			context.Custom["a\"b"] = "customValue";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"custom\":{\"a_b\":\"customValue\"}}");

			context = new Context();
			context.Custom["a*b"] = "customValue";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"custom\":{\"a_b\":\"customValue\"}}");

			context = new Context();
			context.Custom["a*b"] = "customValue1";
			context.Custom["a\"b_c"] = "customValue2";
			json = SerializePayloadItem(context);
			json.Should().Be("{\"custom\":{\"a_b\":\"customValue1\",\"a_b_c\":\"customValue2\"}}");
		}

		[Fact]
		public void MetricSet_Serializes_And_Deserializes()
		{
			var samples = new List<MetricSample>
			{
				new MetricSample("sample_1", 1),
				new MetricSample("sample*\"2", 2),
				new MetricSample("sample_1", 3),
			};

			var metricSet = new Metrics.MetricSet(1603343944891, samples);
			var json = SerializePayloadItem(metricSet);

			json.Should().Be("{\"samples\":{\"sample_1\":{\"value\":1.0},\"sample__2\":{\"value\":2.0}},\"timestamp\":1603343944891}");

			var deserialized = JsonConvert.DeserializeObject<Metrics.MetricSet>(json);
			deserialized.TimeStamp.Should().Be(metricSet.TimeStamp);
			deserialized.Samples.Count().Should().Be(2);
			var count = 0;
			foreach (var sample in deserialized.Samples)
			{
				sample.KeyValue.Key.Should().Be(samples[count].KeyValue.Key.Replace("*", "_").Replace("\"", "_"));
				sample.KeyValue.Value.Should().Be(samples[count].KeyValue.Value);
				++count;
			}
		}

		private string SerializePayloadItem(object item) =>
			_payloadItemSerializer.SerializeObject(item);

		/// <summary>
		/// A dummy type for tests.
		/// </summary>
		private class DummyType
		{
			// ReSharper disable once CollectionNeverQueried.Local - it's by JsonConvert
			public Dictionary<string, string> DictionaryProp { get; } = new Dictionary<string, string>();
			public int IntProp { get; set; }

			public string StringProp { get; set; }
		}
	}
}
