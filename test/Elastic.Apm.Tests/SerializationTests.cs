using System.Text;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Mocks;
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
		/// Tests the <see cref="StringTruncationValueResolver"/>. It serializes a transaction with Transaction.Name.Length > 1024
		/// And makes sure that the Transaction.Name is truncated correctly.
		/// </summary>
		[Fact]
		public void StringTruncationTest()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < 1200; i++)
			{
				sb.Append('a');
			}

			var transaction = new Transaction(new TestAgentComponents(), sb.ToString(), "test") { Duration = 1, Result = "fail" };

			var settings = new JsonSerializerSettings { ContractResolver = new StringTruncationValueResolver() };
			var json = JsonConvert.SerializeObject(transaction, settings);
			var deserializedTransaction = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedTransaction);

			Assert.Equal(Consts.PropertyMaxLength, deserializedTransaction["name"]?.Value<string>()?.Length);
			Assert.Equal("...", deserializedTransaction["name"].Value<string>().Substring(1021, 3));
			Assert.Equal("test", deserializedTransaction["type"].Value<string>());
			Assert.Equal("fail", deserializedTransaction["result"].Value<string>());
		}

		/// <summary>
		/// Test <see cref="NoTruncationInJsonNetAttribute"/>.
		/// It creates an instance of <see cref="DummyType"/> with a <see cref="DummyType.StringProp"/> that has a string
		/// which is longer than <see cref="Consts.PropertyMaxLength"/>.
		/// Makes sure that the serialized instance still contains the whole string (aka it was not trimmed), since
		/// the property pointing to the string was marked with <see cref="NoTruncationInJsonNetAttribute"/>.
		/// </summary>
		[Fact]
		public void NoTruncateAttributeTest()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < 1200; i++)
			{
				sb.Append('a');
			}

			var dummyInstance = new DummyType { IntProp = 42, StringProp = sb.ToString() };

			var settings = new JsonSerializerSettings { ContractResolver = new StringTruncationValueResolver() };
			var json = JsonConvert.SerializeObject(dummyInstance, settings);
			var deserializedDummyInstance = JsonConvert.DeserializeObject<DummyType>(json);

			Assert.NotNull(deserializedDummyInstance);

			Assert.Equal(sb.Length, deserializedDummyInstance.StringProp.Length);
			Assert.Equal(sb.ToString(), deserializedDummyInstance.StringProp);
			Assert.Equal(42, deserializedDummyInstance.IntProp);
		}

		private class DummyType
		{
			[NoTruncationInJsonNet]
			public string StringProp { get; set; }

			public int IntProp { get; set; }
		}
	}
}
