---
navigation_title: "Breaking changes"
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Elastic APM .NET Agent breaking changes [elastic-apm-net-agent-breaking-changes]
Before you upgrade, carefully review the Elastic APM .NET Agent breaking changes and take the necessary steps to mitigate any issues.

To learn how to upgrade, check out [Upgrading](/reference/upgrading.md).

% ## Next version [elastic-apm-net-agent-nextversion-breaking-changes]
% **Release date:** Month day, year

% ::::{dropdown} Title of breaking change
% Description of the breaking change.
% For more information, check [PR #](PR link).
% **Impact**<br> Impact of the breaking change.
% **Action**<br> Steps for mitigating deprecation impact.
% ::::

## 1.33.0 [elastic-apm-net-agent-1330-breaking-changes]
**Release date:** August 19, 2025

This release bumps the minimum `MongoDb.Driver` package to 3.0.0 to unblock consumers who wish to use the latest MongoDb versions. 3.0+. In 3.0, types were moved from `MongoDb.Driver.Core` into `MongoDb.Driver` and deprecated types were removed. To avoid type conflicts, consumers using verions of MongoDb < 3.0.0 will need to first update MongoDb NuGet packages, before updating to this release of Elastic APM agent.

## 1.31.0 [elastic-apm-net-agent-1310-breaking-changes]
**Release date:** December 2, 2024

We no longer ship net6.0 targets as .NET 6 is now out of support. Applications targetting net6.0 will continue to work, but fall down to the netstandard2.0 target which may not be as optimised. We therefore recommend updating your application to net8.0 or net9.0 prior to installing 1.31.0 of the Elastic.Apm.* packages.

For more information, check [#2498](https://github.com/elastic/apm-agent-dotnet/pull/2498).

## 1.29.0 [elastic-apm-net-agent-1290-breaking-changes]
**Release date:** September 18, 2024

This release includes a breaking change in how we parse and send transaction cookies. In 1.26.0, we introduced improved cookie redaction based on the SanitizeFieldNames configuration. To implement this, we extracted each cookie from the Cookie header, storing them in a cookie dictionary on the transaction request data. We have identified a problem with the storage of cookies that include period characters due to the mapping of such data when stored in the APM data stream. This behaviour can lead to lost transactions on requests which include such cookies. This is common in ASP.NET Core due to the default cookie names used for sessions, authentication, etc.

In this release, we no longer parse out individual cookies, and the cookie Dictionary has been removed from the data model. This means that cookies will no longer be indexed individually. However, we have ensured that we retain the primary reason for the earlier change, which was to redact the values of sensitive cookies. Any cookies with a name matching the SanitizeFieldNames patterns will be redacted in the value of the Cookie header we store.

For most consumers, we expect the impact to be minimal. However, if you were relying on the parsed cookie fields, adjustments will be necessary to work with the Cookie header value instead.

No longer parse request cookies, but ensure they are still redacted in the Cookie header string.

For more information, check [#2444](https://github.com/elastic/apm-agent-dotnet/pull/2444).

## 1.21.0 [elastic-apm-net-agent-1210-breaking-changes]
**Release date:** April 5, 2023

This release includes two breaking changes that have minimal impact.

We removed support for target frameworks which have gone into end-of-life support by Microsoft. The impact should be minimal, however as we continue to support netstandard2.0 and netstandard2.1 where applicable.
We removed the collection of GC metrics over ETW on .NET Full Framework. The collection over ETW requires elevated privileges, especially in IIS deployments. This runs counter to best practices. Since these are currently not displayed in the APM UI, while technically breaking, the impact should be minimal. The GC metric collection on modern .NET platforms is not impacted.

* Remove ETW powered GC metrics on FullFramework. For more information, check [#2036](https://github.com/elastic/apm-agent-dotnet/pull/2036).
* Remove unsupported TFM’s. For more information, check [#2027](https://github.com/elastic/apm-agent-dotnet/pull/2027).

## 1.14.0 [elastic-apm-net-agent-1140-breaking-changes]
**Release date:** February 9, 2022

Change unknown service.name to align with other agents. In the very rare cases when the agent is not able to autoamtically detect the name of a service, or it’s not manually set, it’ll use the default service name unknown-dotnet-service. In prior versions this was just unknown.

For more information, check [#1586](https://github.com/elastic/apm-agent-dotnet/pull/1586) and [#1585](https://github.com/elastic/apm-agent-dotnet/issues/1585).

## 1.12.0 [elastic-apm-net-agent-1120-breaking-changes]

Auto-infer destination.service.resource and adapt public API.

`boolean isExitSpan` parameter introduced to Start* and Capture* public APIs to denote when a span is an exit span.

For more information, check [#1520](https://github.com/elastic/apm-agent-dotnet/pull/1520) and [#1330](https://github.com/elastic/apm-agent-dotnet/issues/1330).

## 1.10.0 [elastic-apm-net-agent-1100-breaking-changes]

Do not capture HTTP child spans for Elasticsearch.

For more information, check [#1306](https://github.com/elastic/apm-agent-dotnet/pull/1306) and [#1276](https://github.com/elastic/apm-agent-dotnet/pull/1276).

## 1.9.0 [elastic-apm-net-agent-190-breaking-changes]

The agent tries to never throw any exception. Specifically instead of throwing InstanceAlreadyCreatedException, it will print an error log.

For more information, check [#1161](https://github.com/elastic/apm-agent-dotnet/pull/1161) and [#1162](https://github.com/elastic/apm-agent-dotnet/pull/1162).

## 1.7.0 [elastic-apm-net-agent-170-breaking-changes]

Binary compatibility on `IExecutionSegment.CaptureException` and `IExecutionSegment.CaptureError` with libraries depending on previous version. If this happens you need to update Elastic.Apm to 1.7.0 in your projects.

For more information, check [#1067](https://github.com/elastic/apm-agent-dotnet/issues/1067).

## 1.4.0 [elastic-apm-net-agent-140-breaking-changes]

We have some changes that are technically breaking changes. We made some helper classes internal that were never meant to be public. These are: `Elastic.Apm.Helpers.AgentTimeInstant`, `Elastic.Apm.Helpers.ContractExtensions`, `Elastic.Apm.Helpers.ObjectExtensions`, `Elastic.Apm.Helpers.ToStringBuilder`. None of these classes were documented or mentioned as part of the Public Agent API. We expect no usage of these classes outside the agent.

## 1.0.0 [elastic-apm-net-agent-100-breaking-changes]

We have some breaking changes in this release. We wanted to do these changes prior to our GA release and with this we hopefully avoid breaking changes in the upcoming versions.

* For better naming we replaced the Elastic.Apm.All packages with Elastic.Apm.NetCoreAll. For more information, check [#371](https://github.com/elastic/apm-agent-dotnet/pull/371).
* Based on feedback we also renamed the UseElasticApm() method in the Elastic.Apm.NetCoreAll package to UseAllElasticApm - this method turns on every component of the Agent for ASP.NET Core. For more information, check [#371](https://github.com/elastic/apm-agent-dotnet/pull/371).
* Our logger abstraction, specifically the IApmLogger interface changed. For more information, check [#389](https://github.com/elastic/apm-agent-dotnet/pull/389).
* To follow the [Elastic Common Schema (ECS)][Elastic Common Schema (ECS)](ecs://reference/index.md)), we renamed our Tags properties to Labels. For more information, check [#416](https://github.com/elastic/apm-agent-dotnet/pull/416).