using System;
using System.Linq;
using System.Reflection;

namespace Elastic.Apm.Helpers
{
	internal class PropertyFetcher
	{
		private readonly string _propertyName;
		private PropertyFetch _innerFetcher;

		public PropertyFetcher(string propertyName)
		{
			if (string.IsNullOrWhiteSpace(propertyName))
				throw new ArgumentException("The value must be non-empty, non-null or non-whitespace", nameof(propertyName));

			_propertyName = propertyName;
		}

		public virtual object Fetch(object obj)
		{
			if (_innerFetcher == null)
			{
				var type = obj.GetType().GetTypeInfo();
				var property = type.DeclaredProperties.FirstOrDefault(p =>
					string.Equals(p.Name, _propertyName, StringComparison.InvariantCultureIgnoreCase));
				if (property == null)
				{
					property = type.GetProperty(_propertyName);
				}

				_innerFetcher = PropertyFetch.FetcherForProperty(property);
			}

			return _innerFetcher?.Fetch(obj);
		}

		// see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs
		private class PropertyFetch
		{
			/// <summary>
			/// Create a property fetcher from a .NET Reflection PropertyInfo class that
			/// represents a property of a particular type.
			/// </summary>
			public static PropertyFetch FetcherForProperty(PropertyInfo propertyInfo)
			{
				if (propertyInfo == null)
				{
					// returns null on any fetch.
					return new PropertyFetch();
				}

				var typedPropertyFetcher = typeof(TypedFetchProperty<,>);
				var instantiatedTypedPropertyFetcher = typedPropertyFetcher.GetTypeInfo()
					.MakeGenericType(
						propertyInfo.DeclaringType, propertyInfo.PropertyType);
				return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, propertyInfo);
			}

			/// <summary>
			/// Given an object, fetch the property that this propertyFetch represents.
			/// </summary>
			public virtual object Fetch(object obj) => null;

			private class TypedFetchProperty<TObject, TProperty> : PropertyFetch
			{
				private readonly Func<TObject, TProperty> _propertyFetch;

				public TypedFetchProperty(PropertyInfo property) =>
					_propertyFetch = (Func<TObject, TProperty>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));

				public override object Fetch(object obj) => _propertyFetch((TObject)obj);
			}
		}
	}
}
