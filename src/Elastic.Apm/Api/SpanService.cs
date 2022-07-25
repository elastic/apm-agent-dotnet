// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Service related information can be sent per span. Information provided here will override the more generic information retrieved from metadata,
	/// missing service fields will be retrieved from the metadata information.
	/// </summary>
	public class SpanService
	{
		public Target Target { get; }

		public SpanService(Target target) => Target = target;
	}

	/// <summary>
	/// Target holds information about the outgoing service in case of an outgoing event.
	/// </summary>
	public class Target : IEquatable<Target>
	{
		/// <summary>
		/// Immutable type of the target service for the event.
		/// </summary>
		public string Type { get; private set; }

		/// <summary>
		/// Immutable name of the target service for the event.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Indicates to only use <see cref="Name"/> in <see cref="ToDestinationServiceResource"/>.
		/// E.g. HTTP spans only use name in `Destination.Service.Resource`.
		/// </summary>
		private readonly bool _onlyUseName;

		private Target() { }

		public Target(string type, string name) => (Type, Name) = (type, name);

		internal Target(string type, string name, bool onlyUseName = false) => (Type, Name, _onlyUseName) = (type, name, onlyUseName);

		public static Target TargetWithName(string name) => new Target { Name = name };

		public static Target TargetWithType(string type) => new Target { Type = type };

		public string ToDestinationServiceResource()
		{
			var sb = new StringBuilder();

			if (!_onlyUseName)
			{
				if (!string.IsNullOrEmpty(Type))
					sb.Append(Type);
				if (string.IsNullOrEmpty(Name)) return sb.ToString();

				if (sb.Length > 0)
					sb.Append("/");
			}
			sb.Append(Name);
			return sb.ToString();
		}

		public bool Equals(Target other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Type == other.Type && Name == other.Name;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((Target)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				// ReSharper disable NonReadonlyMemberInGetHashCode
				return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
			}
		}

		public static bool operator ==(Target left, Target right) => Equals(left, right);

		public static bool operator !=(Target left, Target right) => !Equals(left, right);
	}
}
