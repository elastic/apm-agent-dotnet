using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.TestHelpers
{
	/// <summary>
	/// Taken from https://andrewlock.net/creating-strongly-typed-xunit-theory-test-data-with-theorydata/
	/// </summary>
	public abstract class TheoryData : IEnumerable<object[]>
	{
		readonly List<object[]> data = new List<object[]>();

		protected void AddRow(params object[] values)
		{
			data.Add(values);
		}

		public IEnumerator<object[]> GetEnumerator()
		{
			return data.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class TheoryData<T> : TheoryData
	{
		internal void Add(T p)
		{
			AddRow(p);
		}
	}

	public class TheoryData<T1, T2> : TheoryData
	{
		internal void Add(T1 p1, T2 p2)
		{
			AddRow(p1, p2);
		}
	}

	public class TheoryData<T1, T2, T3> : TheoryData
	{
		internal void Add(T1 p1, T2 p2, T3 p3)
		{
			AddRow(p1, p2, p3);
		}
	}


	public class TheoryData<T1, T2, T3, T4> : TheoryData
	{
		internal void Add(T1 p1, T2 p2, T3 p3, T4 p4)
		{
			AddRow(p1, p2, p3, p4);
		}
	}

	public class TheoryData<T1, T2, T3, T4, T5> : TheoryData
	{
		internal void Add(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
		{
			AddRow(p1, p2, p3, p4, p5);
		}
	}

	public class TheoryDataTests
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData<int> TheoryData1 => new TheoryData<int>
		{
			{ 137 * 2},
			{ 137 * 3 },
			{ 137 * 4 },
		};

		[Theory]
		[MemberData(nameof(TheoryData1))]
		public void TestTheoryData1(int p)
		{
			(p % 137).Should().Be(0);
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData<int, string> TheoryData2 => new TheoryData<int, string>
		{
			{ 123, "7B" },
			{ 987654, "F1206" },
			{ int.MaxValue, "7FFFFFFF" }
		};

		[Theory]
		[MemberData(nameof(TheoryData2))]
		public void TestTheoryData2(int i, string iHex)
		{
			i.ToString("X").Should().Be(iHex);
		}

		private struct TypedInt<TType>
		{
			internal readonly int Value;

			internal TypedInt(int value)
			{
				Value = value;
			}
		}

		private struct TA { };
		private struct TB { };
		private struct TC { };
		private struct TD { };
		private struct TE { };

		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData TheoryData3 => new TheoryData<TypedInt<TA>, TypedInt<TB>, TypedInt<TC>>
		{
			{ new TypedInt<TA>(1), new TypedInt<TB>(2), new TypedInt<TC>(3) },
			{ new TypedInt<TA>(123), new TypedInt<TB>(456), new TypedInt<TC>(579) },
			{ new TypedInt<TA>(789), new TypedInt<TB>(1230), new TypedInt<TC>(2019) },
		};

		[Theory]
		[MemberData(nameof(TheoryData3))]
		private void TestTheoryData3(TypedInt<TA> p1, TypedInt<TB> p2, TypedInt<TC> p3)
		{
			(p1.Value + p2.Value).Should().Be(p3.Value);
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData TheoryData4 => new TheoryData<TypedInt<TA>, TypedInt<TB>, TypedInt<TC>, TypedInt<TD>>
		{
			{ new TypedInt<TA>(1), new TypedInt<TB>(2), new TypedInt<TC>(3), new TypedInt<TD>(6) },
			{ new TypedInt<TA>(4), new TypedInt<TB>(5), new TypedInt<TC>(6), new TypedInt<TD>(120) },
		};

		[Theory]
		[MemberData(nameof(TheoryData4))]
		private void TestTheoryData4(TypedInt<TA> p1, TypedInt<TB> p2, TypedInt<TC> p3, TypedInt<TD> p4)
		{
			(p1.Value * p2.Value * p3.Value).Should().Be(p4.Value);
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData TheoryData5 => new TheoryData<TypedInt<TA>, TypedInt<TB>, TypedInt<TC>, TypedInt<TD>, TypedInt<TE>>
		{
			{ new TypedInt<TA>(1), new TypedInt<TB>(2), new TypedInt<TC>(3), new TypedInt<TD>(4), new TypedInt<TE>(10) },
			{ new TypedInt<TA>(5), new TypedInt<TB>(6), new TypedInt<TC>(7), new TypedInt<TD>(8), new TypedInt<TE>(26) },
		};

		[Theory]
		[MemberData(nameof(TheoryData5))]
		private void TestTheoryData5(TypedInt<TA> p1, TypedInt<TB> p2, TypedInt<TC> p3, TypedInt<TD> p4, TypedInt<TE> p5)
		{
			(p1.Value + p2.Value + p3.Value + p4.Value).Should().Be(p5.Value);
		}
	}
}
