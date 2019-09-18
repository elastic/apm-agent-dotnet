using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;
// ReSharper disable ImplicitlyCapturedClosure

namespace Elastic.Apm.Tests.HelpersTests
{
	public class AssertionTests
	{
		[Fact]
		internal void level_Disabled()
		{
			var assertion = new Assertion.Impl(AssertionLevel.Disabled);

			assertion.IsEnabled.Should().BeFalse();
			assertion.Is_O_n_LevelEnabled.Should().BeFalse();

			var counter = 0;

			assertion.IfEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(0);

			assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}");
			counter.Should().Be(0);

			assertion.If_O_n_LevelEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(0);

			assertion.If_O_n_LevelEnabled?.That(++counter != 1, $"Dummy message {++counter}");
			counter.Should().Be(0);

			assertion.DoIfEnabled(assert =>
			{
				assert.That(++counter == 1, $"Dummy message {++counter}");
				counter.Should().Be(0);

				assert.That(++counter != 1, $"Dummy message {++counter}");
				counter.Should().Be(0);
			});

			assertion.DoIfEnabled(assert =>
			{
				assert.That(++counter == 1, $"Dummy message {++counter}");
				counter.Should().Be(0);

				assert.That(++counter != 1, $"Dummy message {++counter}");
				counter.Should().Be(0);
			});
		}

		[Fact]
		internal void level_O_1()
		{
			var assertion = new Assertion.Impl(AssertionLevel.O_1);

			assertion.IsEnabled.Should().BeTrue();
			assertion.Is_O_n_LevelEnabled.Should().BeFalse();

			var counter = 0;
			assertion.IfEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(2);
			counter = 0;

			AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}")).Should()
				.ThrowExactly<AssertionFailedException>()
				.WithMessage("Dummy message 2");
			counter.Should().Be(2);
			counter = 0;

			assertion.If_O_n_LevelEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(0);

			assertion.If_O_n_LevelEnabled?.That(++counter != 1, $"Dummy message {++counter}");
			counter.Should().Be(0);


			assertion.DoIfEnabled(assert =>
			{
				assert.That(++counter == 1, $"Dummy message {++counter}");
				counter.Should().Be(2);
				counter = 0;

				AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}")).Should()
					.ThrowExactly<AssertionFailedException>()
					.WithMessage("Dummy message 2");
			});
			counter.Should().Be(2);
			counter = 0;

			assertion.DoIf_O_n_LevelEnabled(_ => { ++counter; });
			counter.Should().Be(0);
		}

		[Fact]
		internal void level_O_n()
		{
			var assertion = new Assertion.Impl(AssertionLevel.O_n);

			assertion.IsEnabled.Should().BeTrue();
			assertion.Is_O_n_LevelEnabled.Should().BeTrue();

			var counter = 0;
			assertion.IfEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(2);
			counter = 0;

			AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}")).Should()
				.ThrowExactly<AssertionFailedException>()
				.WithMessage("Dummy message 2");
			counter.Should().Be(2);
			counter = 0;

			assertion.If_O_n_LevelEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(2);
			counter = 0;

			AsAction(() => assertion.If_O_n_LevelEnabled?.That(++counter != 1, $"Dummy message {++counter}")).Should()
				.ThrowExactly<AssertionFailedException>()
				.WithMessage("Dummy message 2");
			counter.Should().Be(2);
			counter = 0;

			assertion.DoIfEnabled(assert =>
			{
				assert.That(++counter == 1, $"Dummy message {++counter}");
				counter.Should().Be(2);
				counter = 0;

				AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}")).Should()
					.ThrowExactly<AssertionFailedException>()
					.WithMessage("Dummy message 2");
			});
			counter.Should().Be(2);
			counter = 0;

			assertion.DoIf_O_n_LevelEnabled(assert =>
			{
				assert.That(++counter == 1, $"Dummy message {++counter}");
				counter.Should().Be(2);
				counter = 0;

				AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}")).Should()
					.ThrowExactly<AssertionFailedException>()
					.WithMessage("Dummy message 2");
			});
			counter.Should().Be(2);
			counter = 0;
		}
	}
}
