using System.Collections.Generic;
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
		/// <summary>
		/// Tests the <see cref="TrimmedStringJsonConverter" />. It serializes a transaction with Transaction.Name.Length > 1024.
		/// Makes sure that the Transaction.Name is truncated correctly.
		/// </summary>
		[Fact]
		public void StringTruncationTest()
		{
			var str = new string('a', 1200);

			var transaction =
				new Transaction(new TestAgentComponents(), str, "test", new TestAgentConfigurationReader(new NoopLogger()))
				{
					Duration = 1, Result = "fail"
				};

			var json = SerializePayloadItem(transaction);
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

			Assert.Equal(Consts.PropertyMaxLength, deserializedContext["labels"].Value<JObject>()["foo"]?.Value<string>()?.Length);
			Assert.Equal("...", deserializedContext["labels"].Value<JObject>()["foo"].Value<string>().Substring(1021, 3));
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
			var deserializedDummyInstance = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedDummyInstance);

			Assert.Equal(str.Length, deserializedDummyInstance["dictionaryProp"].Value<JObject>()["foo"]?.Value<string>()?.Length);
			Assert.Equal(str, deserializedDummyInstance["dictionaryProp"].Value<JObject>()["foo"].Value<string>());
		}

		/// <summary>
		/// Creates a db instance with a statement that is longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure the statement is not truncated.
		/// </summary>
		[Fact]
		public void DbStatementLengthTest()
		{
			var str = new string('a', 1200);
			var db = new Database { Statement = str };

			var json = SerializePayloadItem(db);
			var deserializedDb = JsonConvert.DeserializeObject<Database>(json);

			Assert.NotNull(deserializedDb);

			Assert.Equal(str.Length, deserializedDb.Statement.Length);
			Assert.Equal(str, deserializedDb.Statement);
		}

		/// <summary>
		/// Verifies that <see cref="Transaction.Context" /> is serialized only when the transaction is sampled.
		/// </summary>
		[Fact]
		public void TransactionContextShouldBeSerializedOnlyWhenSampled()
		{
			var agent = new TestAgentComponents();
			// Create a transaction that is sampled (because the sampler is constant sampling-everything sampler
			var sampledTransaction = new Transaction(agent.Logger, "dummy_name", "dumm_type", new Sampler(1.0), null, agent.PayloadSender,
				new TestAgentConfigurationReader(new NoopLogger()));
			sampledTransaction.Context.Request = new Request("GET",
				new Url { Full = "https://elastic.co", Raw = "https://elastic.co", HostName = "elastic.co", Protocol = "HTTP" });

			// Create a transaction that is not sampled (because the sampler is constant not-sampling-anything sampler
			var nonSampledTransaction = new Transaction(agent.Logger, "dummy_name", "dumm_type", new Sampler(0.0), null, agent.PayloadSender,
				new TestAgentConfigurationReader(new NoopLogger()));
			nonSampledTransaction.Context.Request = sampledTransaction.Context.Request;

			var serializedSampledTransaction = SerializePayloadItem(sampledTransaction);
			var deserializedSampledTransaction = JsonConvert.DeserializeObject(serializedSampledTransaction) as JObject;
			var serializedNonSampledTransaction = SerializePayloadItem(nonSampledTransaction);
			var deserializedNonSampledTransaction = JsonConvert.DeserializeObject(serializedNonSampledTransaction) as JObject;

			deserializedSampledTransaction["sampled"].Value<bool>().Should().BeTrue();
			deserializedSampledTransaction["context"].Value<JObject>()["request"].Value<JObject>()["url"].Value<JObject>()["full"]
				.Should()
				.Equals("https://elastic.co");

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
		public void SerializationUtilsTrimToLengthTests(string original, int maxLength, string expectedTrimmed) =>
			SerializationUtils.TrimToLength(original, maxLength).Should().Be(expectedTrimmed);

		public static IEnumerable<object[]> SerializationUtilsTrimToPropertyMaxLengthVariantsToTest()
		{
			yield return new object[] { "", "" };
			yield return new object[] { "A", "A" };
			yield return new object[] { "B".Repeat(Consts.PropertyMaxLength), "B".Repeat(Consts.PropertyMaxLength) };
			yield return new object[] { "C".Repeat(Consts.PropertyMaxLength + 1), "C".Repeat(Consts.PropertyMaxLength - 3) + "..." };
			yield return new object[] { "D".Repeat(Consts.PropertyMaxLength * 2), "D".Repeat(Consts.PropertyMaxLength - 3) + "..." };
		}

		[Theory]
		[MemberData(nameof(SerializationUtilsTrimToPropertyMaxLengthVariantsToTest))]
		public void SerializationUtilsTrimToPropertyMaxLengthTests(string original, string expectedTrimmed)
		{
			Consts.PropertyMaxLength.Should().BeGreaterThan(3);
			SerializationUtils.TrimToPropertyMaxLength(original).Should().Be(expectedTrimmed);
		}

		private static string SerializePayloadItem(object item) =>
			new PayloadItemSerializer().SerializeObject(item);

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
