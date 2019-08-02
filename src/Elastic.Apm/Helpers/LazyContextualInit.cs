using System;
using System.Threading;

namespace Elastic.Apm.Helpers
{
	internal class LazyContextualInit<T>
	{
		private T _value;
		private bool _isInited;

		/// <summary>
		/// Lock object is created on demand by LazyInitializer.EnsureInitialized
		/// https://docs.microsoft.com/en-us/dotnet/api/system.threading.lazyinitializer.ensureinitialized?view=netframework-4.8#System_Threading_LazyInitializer_EnsureInitialized__1___0__System_Boolean__System_Object__System_Func___0__
		/// </summary>
		private object _lock;

		internal bool IsInited => Volatile.Read(ref _isInited);

		internal T Value => _value;

		/// <summary>
		/// This method allows to optimize creating <c>producer</c>
		/// To use in the following manner: <c>ctxLazy.IfNotInited?.InitOrGet( ... ) ?? ctxLazy.Value</c>
		/// <returns><c>null</c> if value is already initialized and some non-<c>null</c> object otherwise</returns>>
		/// </summary>
		internal IfNotInitedHelper? IfNotInited => IsInited ? (IfNotInitedHelper?)null : new IfNotInitedHelper(this);

		internal T InitOrGet(Func<T> producer)
		{
			producer.ThrowIfArgumentNull(nameof(producer));

			return LazyInitializer.EnsureInitialized(ref _value, ref _isInited, ref _lock, producer);
		}

		internal readonly struct IfNotInitedHelper
		{
			private readonly LazyContextualInit<T> _owner;

			internal IfNotInitedHelper(LazyContextualInit<T> owner)
			{
				_owner = owner;
			}

			internal T InitOrGet(Func<T> producer) => _owner.InitOrGet(producer);
		}
	}
}
