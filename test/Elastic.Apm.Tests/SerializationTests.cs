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

			var transaction = new Transaction(new TestAgentComponents(), sb.ToString(), "test"){ Duration =  1, Result = "fail"};

			var settings = new JsonSerializerSettings { ContractResolver = new StringTruncationValueResolver() };
			var json = JsonConvert.SerializeObject(transaction, settings);
			var deserializedTransaction = JsonConvert.DeserializeObject(json) as JObject;

			Assert.NotNull(deserializedTransaction);

			Assert.Equal(Consts.PropertyMaxLength, deserializedTransaction["name"]?.Value<string>()?.Length);
			Assert.Equal("...", deserializedTransaction["name"].Value<string>().Substring(1021, 3));
			Assert.Equal("test", deserializedTransaction["type"].Value<string>());
			Assert.Equal("fail", deserializedTransaction["result"].Value<string>());
		}
	}
}
