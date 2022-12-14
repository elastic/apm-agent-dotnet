// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Model;
using FluentAssertions;

namespace Elastic.Apm.Tests.MockApmServer;

internal static class AssertValidExtensions
{
	internal static void AssertValid(this Service thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Agent.AssertValid();
		thisObj.Framework?.AssertValid();
		thisObj.Language?.AssertValid();
	}

	internal static void AssertValid(this Service.AgentC thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Name.Should().Be(Consts.AgentName);
		thisObj.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+");
	}

	private static void AssertValid(this Framework thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Name.NonEmptyAssertValid();
		thisObj.Version.NonEmptyAssertValid();
		thisObj.Version.Should().MatchRegex("[1-9]*.*");
	}

	private static void AssertValid(this Language thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Name.Should().Be("C#");
	}

	internal static void AssertValid(this Api.System thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Container?.AssertValid();
	}

	private static void AssertValid(this Container thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Id.NonEmptyAssertValid();
	}

	internal static void AssertValid(this Request thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Headers?.HttpHeadersAssertValid();
		thisObj.HttpVersion?.HttpVersionAssertValid();
		thisObj.Method.HttpMethodAssertValid();
		thisObj.Socket?.AssertValid();
		thisObj.Url.AssertValid();
	}

	internal static void AssertValid(this Response thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Headers?.HttpHeadersAssertValid();
		thisObj.StatusCode.HttpStatusCodeAssertValid();
	}

	internal static void HttpStatusCodeAssertValid(this int thisObj) =>
		thisObj.Should().BeInRange(100, 599);

	internal static void AssertValid(this User thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj?.Id.AssertValid();
		thisObj?.UserName.AssertValid();
		thisObj?.Email.AssertValid();
	}

	internal static void LabelsAssertValid(this LabelsDictionary thisObj)
	{
		thisObj.Should().NotBeNull();

		foreach (var (key, value) in thisObj.MergedDictionary)
		{
			key.AssertValid();
			value?.AssertValid();
		}
	}

	private static void HttpHeadersAssertValid(this Dictionary<string, string> thisObj)
	{
		thisObj.Should().NotBeNull();

		foreach (var headerNameValue in thisObj)
		{
			headerNameValue.Key.Should().NotBeNullOrEmpty();
			headerNameValue.Value.Should().NotBeNull();
		}
	}

	private static void HttpVersionAssertValid(this string thisObj)
	{
		thisObj.NonEmptyAssertValid();
		thisObj.Should().BeOneOf("1.0", "1.1", "2.0", "2");
	}

	private static void HttpMethodAssertValid(this string thisObj)
	{
		thisObj.NonEmptyAssertValid();
		var validValues = new List<string>
		{
			"GET",
			"POST",
			"PUT",
			"HEAD",
			"DELETE",
			"CONNECT",
			"OPTIONS",
			"TRACE",
			"PATCH"
		};
		thisObj.ToUpperInvariant().Should().BeOneOf(validValues, $"Given value is `{thisObj}'");
	}

	private static void AssertValid(this Url thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Raw?.UrlStringAssertValid();
		thisObj.Protocol?.UrlProtocolAssertValid();
		thisObj.Full?.UrlStringAssertValid();
		thisObj.HostName?.AssertValid();
		thisObj.PathName?.AssertValid();
		thisObj.Search?.AssertValid();
	}

	private static void UrlProtocolAssertValid(this string thisObj)
	{
		thisObj.NonEmptyAssertValid();
		thisObj.Should().Be("HTTP");
	}

	private static void UrlStringAssertValid(this string thisObj)
	{
		thisObj.NonEmptyAssertValid();
		thisObj.Should()
			.Match(s =>
				s.StartsWith("http://", StringComparison.InvariantCulture) ||
				s.StartsWith("https://", StringComparison.InvariantCulture));
	}

	private static void AssertValid(this Socket thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.RemoteAddress?.NonEmptyAssertValid();
	}

	internal static void AssertValid(this SpanCountDto thisObj) => thisObj.Should().NotBeNull();

	internal static void TimestampAssertValid(this long thisObj) => thisObj.Should().BeGreaterOrEqualTo(0);

	internal static void TraceIdAssertValid(this string thisObj) => thisObj.HexAssertValid(128 /* bits */);

	internal static void TransactionIdAssertValid(this string thisObj) => thisObj.HexAssertValid(64 /* bits */);

	internal static void SpanIdAssertValid(this string thisObj) => thisObj.HexAssertValid(64 /* bits */);

	internal static void ParentIdAssertValid(this string thisObj) => thisObj.HexAssertValid(64 /* bits */);

	internal static void ErrorIdAssertValid(this string thisObj) => thisObj.HexAssertValid(128 /* bits */);

	internal static void DurationAssertValid(this double thisObj) => thisObj.Should().BeGreaterOrEqualTo(0);

	internal static void NameAssertValid(this string thisObj) => thisObj.NonEmptyAssertValid();

	private static void HexAssertValid(this string thisObj, int sizeInBits)
	{
		thisObj.NonEmptyAssertValid();
		(sizeInBits % 8).Should().Be(0, $"sizeInBits should be divisible by 8 but sizeInBits is {sizeInBits}");
		var numHexChars = sizeInBits / 8 * 2;
		var because =
			$"String should be {numHexChars} hex digits ({sizeInBits}-bits) but the actual value is `{thisObj}' (length: {thisObj.Length})";
		thisObj.Length.Should().Be(numHexChars, because); // 2 hex chars per byte
		TraceContext.IsHex(thisObj).Should().BeTrue(because);
	}

	internal static void NonEmptyAssertValid(this string thisObj, int maxLength = 1024)
	{
		thisObj.AssertValid();
		thisObj.Should().NotBeEmpty();
	}

	internal static void AssertValid(this string thisObj, int maxLength = 1024)
	{
		thisObj.UnlimitedLengthAssertValid();
		thisObj.Length.Should().BeLessOrEqualTo(maxLength);
	}

	internal static void AssertValid(this Label thisObj)
	{
		thisObj.Should().NotBeNull();
		thisObj.Value.Should().NotBeNull();
	}

	internal static void UnlimitedLengthAssertValid(this string thisObj) => thisObj.Should().NotBeNull();

	internal static void AssertValid(this CapturedException thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Code?.AssertValid();
		thisObj.StackTrace?.AssertValid();

		if (string.IsNullOrEmpty(thisObj.Type))
			thisObj.Message.AssertValid();
		else
			thisObj.Message?.AssertValid();

		if (string.IsNullOrEmpty(thisObj.Message))
			thisObj.Type.AssertValid();
		else
			thisObj.Type?.AssertValid();
	}

	internal static void AssertValid(this Database thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Instance?.AssertValid();
		thisObj.Statement?.AssertValid(10_000);
		thisObj.Type?.AssertValid();
	}

	internal static void AssertValid(this Http thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Method.HttpMethodAssertValid();
		thisObj.StatusCode.HttpStatusCodeAssertValid();
		thisObj.Url.Should().NotBeNullOrEmpty();
	}

	internal static void AssertValid(this Destination thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Address?.AssertValid();
		thisObj.Port?.Should().BeGreaterOrEqualTo(0);
	}

	internal static void AssertValid(this List<CapturedStackFrame> thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.Should().NotBeEmpty();
		foreach (var stackFrame in thisObj) stackFrame.AssertValid();
	}

	internal static void AssertValid(this CapturedStackFrame thisObj)
	{
		thisObj.Should().NotBeNull();

		thisObj.FileName.Should().NotBeNullOrEmpty();
		thisObj.LineNo.Should().BeGreaterOrEqualTo(0);
		thisObj.Module?.Should().NotBeEmpty();
		thisObj.Function?.Should().NotBeEmpty();
	}
}
