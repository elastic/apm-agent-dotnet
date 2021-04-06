// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Elastic.Apm.Reflection
{
	internal class ExpressionBuilder
	{
		/// <summary>
		/// Builds a delegate to get a property of type <typeparamref name="TProperty"/> from an object
		/// of type <typeparamref name="TObject"/>
		/// </summary>
		public static Func<TObject, TProperty> BuildPropertyGetter<TObject, TProperty>(string propertyName)
		{
			var parameterExpression = Expression.Parameter(typeof(TObject), "value");
			var memberExpression = Expression.Property(parameterExpression, propertyName);
			return Expression.Lambda<Func<TObject, TProperty>>(memberExpression, parameterExpression).Compile();
		}

		/// <summary>
		/// Builds a delegate to get a property from an object. <paramref name="type"/> is cast to <see cref="Object"/>,
		/// with the returned property cast to <see cref="Object"/>.
		/// </summary>
		public static Func<object, object> BuildPropertyGetter(Type type, PropertyInfo propertyInfo)
		{
			var parameterExpression = Expression.Parameter(typeof(object), "value");
			var parameterCastExpression = Expression.Convert(parameterExpression, type);
			var memberExpression = Expression.Property(parameterCastExpression, propertyInfo);
			var returnCastExpression = Expression.Convert(memberExpression, typeof(object));
			return Expression.Lambda<Func<object, object>>(returnCastExpression, parameterExpression).Compile();
		}

		/// <summary>
		/// Builds a delegate to get a property from an object. <paramref name="type"/> is cast to <see cref="Object"/>,
		/// with the returned property cast to <see cref="Object"/>.
		/// </summary>
		public static Func<object, object> BuildPropertyGetter(Type type, string propertyName)
		{
			var parameterExpression = Expression.Parameter(typeof(object), "value");
			var parameterCastExpression = Expression.Convert(parameterExpression, type);
			var memberExpression = Expression.Property(parameterCastExpression, propertyName);
			var returnCastExpression = Expression.Convert(memberExpression, typeof(object));
			return Expression.Lambda<Func<object, object>>(returnCastExpression, parameterExpression).Compile();
		}
	}
}
