using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Makes sure that <see cref="LogValuesFormatter"/> builds correct logstate
	/// </summary>
	public class LogValuesFormatterTests
	{
		[Fact]
		public void SimpleStateTest()
		{
			object[] parameters = { "123" };
			var logValuesFormatter = new LogValuesFormatter("MyLog {template}", parameters);
			var state = logValuesFormatter.GetState(parameters);

			state.Count.Should().Be(2);

			state[0].Key.Should().Be("{OriginalFormat}");
			state[0].Value.Should().Be("MyLog {template}");

			state[1].Key.Should().Be("template");
			state[1].Value.Should().Be("123");
		}

		[Fact]
		public void MultipleParametersTest()
		{
			object[] parameters = { "123", "456" };
			var logValuesFormatter = new LogValuesFormatter("MyLog {template1}, {template2}", parameters);
			var state = logValuesFormatter.GetState(parameters);

			state.Count.Should().Be(3);

			state[0].Key.Should().Be("{OriginalFormat}");
			state[0].Value.Should().Be("MyLog {template1}, {template2}");

			state[1].Key.Should().Be("template1");
			state[1].Value.Should().Be("123");

			state[2].Key.Should().Be("template2");
			state[2].Value.Should().Be("456");
		}

		[Fact]
		public void ScopedMultipleParametersTest()
		{
			object[] parameters = { "123", "456" };
			var logValuesFormatter = new LogValuesFormatter("{Scope} MyLog {template1}, {template2}", parameters, nameof(LogValuesFormatterTests));
			var state = logValuesFormatter.GetState(parameters);

			state.Count.Should().Be(4);

			state[0].Key.Should().Be("{OriginalFormat}");
			state[0].Value.Should().Be("{Scope} MyLog {template1}, {template2}");

			state[1].Key.Should().Be("Scope");
			state[1].Value.Should().Be(nameof(LogValuesFormatterTests));

			state[2].Key.Should().Be("template1");
			state[2].Value.Should().Be("123");

			state[3].Key.Should().Be("template2");
			state[3].Value.Should().Be("456");
		}
	}
}
