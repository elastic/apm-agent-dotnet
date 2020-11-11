// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// A simple version implementation based on
// https://github.com/maxhauser/semver/blob/master/src/Semver/SemVersion.cs
// MIT License
// Copyright (c) 2013 Max Hauser
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Elastic.Apm.ServerInfo
{
	/// <summary>
	/// An Elastic product version
	/// </summary>
	internal sealed class ElasticVersion : IEquatable<ElasticVersion>, IComparable<ElasticVersion>, IComparable
	{
		private static readonly Regex VersionRegex = new Regex(
			@"^(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?(\-(?<pre>[0-9A-Za-z]+))?$",
			RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

		public ElasticVersion(string version)
		{
			var match = VersionRegex.Match(version);
			if (!match.Success)
				throw new ArgumentException($"Invalid version '{version}'", nameof(version));

			var major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);

			var minorMatch = match.Groups["minor"];
			var minor = minorMatch.Success
				? int.Parse(minorMatch.Value, CultureInfo.InvariantCulture)
				: 0;

			var patchMatch = match.Groups["patch"];
			var patch = patchMatch.Success
				? int.Parse(patchMatch.Value, CultureInfo.InvariantCulture)
				: 0;

			var prerelease = match.Groups["pre"].Value;

			Major = major;
			Minor = minor;
			Patch = patch;
			Prerelease = prerelease;
		}

		public ElasticVersion(int major, int minor, int patch, string prerelease)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
			Prerelease = prerelease;
		}

		public int Major { get; }

		public int Minor { get; }

		public int Patch { get; }

		public string Prerelease { get; }

		public static bool TryParse(string version, out ElasticVersion elasticVersion)
		{
			try
			{
				elasticVersion = new ElasticVersion(version);
				return true;
			}
			catch (Exception)
			{
				elasticVersion = null;
				return false;
			}
		}

		public static bool Equals(ElasticVersion versionA, ElasticVersion versionB) =>
			ReferenceEquals(versionA, null)
				? ReferenceEquals(versionB, null)
				: versionA.Equals(versionB);

		public static int Compare(ElasticVersion versionA, ElasticVersion versionB) =>
			ReferenceEquals(versionA, null)
				? ReferenceEquals(versionB, null) ? 0 : -1
				: versionA.CompareTo(versionB);

		public ElasticVersion Change(int? major = null, int? minor = null, int? patch = null, string prerelease = null) =>
			new ElasticVersion(
				major ?? Major,
				minor ?? Minor,
				patch ?? Patch,
				prerelease ?? Prerelease);

		public override string ToString()
		{
			var version = "" + Major + "." + Minor + "." + Patch;
			if (!string.IsNullOrEmpty(Prerelease))
				version += "-" + Prerelease;

			return version;
		}

		public int CompareTo(object obj) => CompareTo((ElasticVersion)obj);

		public int CompareTo(ElasticVersion other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			var r = CompareByPrecedence(other);
			return r;
		}

		public bool PrecedenceMatches(ElasticVersion other) => CompareByPrecedence(other) == 0;

		public int CompareByPrecedence(ElasticVersion other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			var r = Major.CompareTo(other.Major);
			if (r != 0) return r;

			r = Minor.CompareTo(other.Minor);
			if (r != 0) return r;

			r = Patch.CompareTo(other.Patch);
			if (r != 0) return r;

			r = CompareComponent(Prerelease, other.Prerelease, true);
			return r;
		}

		private static int CompareComponent(string a, string b, bool lower = false)
		{
			var aEmpty = string.IsNullOrEmpty(a);
			var bEmpty = string.IsNullOrEmpty(b);
			if (aEmpty && bEmpty)
				return 0;

			if (aEmpty)
				return lower ? 1 : -1;
			if (bEmpty)
				return lower ? -1 : 1;

			var aComps = a.Split('.');
			var bComps = b.Split('.');

			var minLen = Math.Min(aComps.Length, bComps.Length);
			for (var i = 0; i < minLen; i++)
			{
				var ac = aComps[i];
				var bc = bComps[i];
				int anum, bnum;
				var isanum = int.TryParse(ac, out anum);
				var isbnum = int.TryParse(bc, out bnum);
				int r;
				if (isanum && isbnum)
				{
					r = anum.CompareTo(bnum);
					if (r != 0) return anum.CompareTo(bnum);
				}
				else
				{
					if (isanum)
						return -1;
					if (isbnum)
						return 1;

					r = string.CompareOrdinal(ac, bc);
					if (r != 0)
						return r;
				}
			}

			return aComps.Length.CompareTo(bComps.Length);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, null))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			var other = (ElasticVersion)obj;

			return Equals(other);
		}

		public bool Equals(ElasticVersion other)
		{
			if (other == null)
				return false;

			return Major == other.Major &&
				Minor == other.Minor &&
				Patch == other.Patch &&
				string.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = Major.GetHashCode();
				result = result * 31 + Minor.GetHashCode();
				result = result * 31 + Patch.GetHashCode();
				result = result * 31 + Prerelease.GetHashCode();
				return result;
			}
		}

		public static bool operator ==(ElasticVersion left, ElasticVersion right) => Equals(left, right);

		public static bool operator !=(ElasticVersion left, ElasticVersion right) => !Equals(left, right);

		public static bool operator >(ElasticVersion left, ElasticVersion right) => Compare(left, right) > 0;

		public static bool operator >=(ElasticVersion left, ElasticVersion right) => left == right || left > right;

		public static bool operator <(ElasticVersion left, ElasticVersion right) => Compare(left, right) < 0;

		public static bool operator <=(ElasticVersion left, ElasticVersion right) => left == right || left < right;
	}
}
