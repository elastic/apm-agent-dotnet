using System.Collections.Generic;
using System.Text;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Mocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
			var sb = new StringBuilder();

			for (var i = 0; i < 1200; i++) sb.Append('a');

			var transaction = new Transaction(new TestAgentComponents(), sb.ToString(), "test") { Duration = 1, Result = "fail" };

			var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var json = JsonConvert.SerializeObject(transaction, settings);
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
			var sb = new StringBuilder();

			for (var i = 0; i < 1200; i++) sb.Append('a');

			var dummyInstance = new DummyType { IntProp = 42, StringProp = sb.ToString() };

			var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var json = JsonConvert.SerializeObject(dummyInstance, settings);
			var deserializedDummyInstance = JsonConvert.DeserializeObject<DummyType>(json);

			Assert.NotNull(deserializedDummyInstance);

			Assert.Equal(sb.Length, deserializedDummyInstance.StringProp.Length);
			Assert.Equal(sb.ToString(), deserializedDummyInstance.StringProp);
			Assert.Equal(42, deserializedDummyInstance.IntProp);
		}

		/// <summary>
		/// Creates a <see cref="Context"/> that has <see cref="Context.Tags"/>
		/// with strings longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure that the long string is trimmed.
		/// </summary>
		[Fact]
		public void TagsTruncation()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < 1200; i++) sb.Append('a');

			var context = new Context();
			context.Tags["foo"] = sb.ToString();

			var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var json = JsonConvert.SerializeObject(context, settings);
			var deserializedContext = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedContext);

			Assert.Equal(Consts.PropertyMaxLength, deserializedContext["tags"].Value<JObject>()["foo"]?.Value<string>()?.Length);
			Assert.Equal("...", deserializedContext["tags"].Value<JObject>()["foo"].Value<string>().Substring(1021, 3));
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
			var sb = new StringBuilder();

			for (var i = 0; i < 1200; i++) sb.Append('a');

			var dummyInstance = new DummyType();
			dummyInstance.DictionaryProp["foo"] = sb.ToString();

			var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var json = JsonConvert.SerializeObject(dummyInstance, settings);
			var deserializedDummyInstance = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedDummyInstance);

			Assert.Equal(sb.Length, deserializedDummyInstance["dictionaryProp"].Value<JObject>()["foo"]?.Value<string>()?.Length);
			Assert.Equal(sb.ToString(), deserializedDummyInstance["dictionaryProp"].Value<JObject>()["foo"].Value<string>());
		}

		/// <summary>
		/// Creates a db instance with a statement that is longer than <see cref="Consts.PropertyMaxLength" />.
		/// Makes sure the statement is not truncated.
		/// </summary>
		[Fact]
		public void DbStatementLengthTest()
		{
			var sb = new StringBuilder();
			for (var i = 0; i < 1200; i++) sb.Append('a');
			var db = new Db { Statement = sb.ToString() };

			var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var json = JsonConvert.SerializeObject(db, settings);
			var deserializedDb = JsonConvert.DeserializeObject<Db>(json);

			Assert.NotNull(deserializedDb);

			Assert.Equal(sb.Length, deserializedDb.Statement.Length);
			Assert.Equal(sb.ToString(), deserializedDb.Statement);
		}

		/// <summary>
		/// A dummy type for tests.
		/// </summary>
		private class DummyType
		{
			public int IntProp { get; set; }

			public string StringProp { get; set; }

			// ReSharper disable once CollectionNeverQueried.Local - it's by JsonConvert
			public Dictionary<string, string> DictionaryProp { get; } = new Dictionary<string, string>();
		}
	}
}
