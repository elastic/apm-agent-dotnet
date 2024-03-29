// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Tests.Utilities.FluentAssertionsUtils;

// ReSharper disable ImplicitlyCapturedClosure
#pragma warning disable NullConditionalAssertion

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

			AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}"))
				.Should()
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

				AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}"))
					.Should()
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

			AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}"))
				.Should()
				.ThrowExactly<AssertionFailedException>()
				.WithMessage("Dummy message 2");
			counter.Should().Be(2);
			counter = 0;

			assertion.If_O_n_LevelEnabled?.That(++counter == 1, $"Dummy message {++counter}");
			counter.Should().Be(2);
			counter = 0;

			AsAction(() => assertion.If_O_n_LevelEnabled?.That(++counter != 1, $"Dummy message {++counter}"))
				.Should()
				.ThrowExactly<AssertionFailedException>()
				.WithMessage("Dummy message 2");
			counter.Should().Be(2);
			counter = 0;

			assertion.DoIfEnabled(assert =>
			{
				assert.That(++counter == 1, $"Dummy message {++counter}");
				counter.Should().Be(2);
				counter = 0;

				AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}"))
					.Should()
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

				AsAction(() => assertion.IfEnabled?.That(++counter != 1, $"Dummy message {++counter}"))
					.Should()
					.ThrowExactly<AssertionFailedException>()
					.WithMessage("Dummy message 2");
			});
			counter.Should().Be(2);
			counter = 0;
		}
	}
}
