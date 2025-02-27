---
navigation_title: "Elastic APM .NET Agent"
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/release-notes-1.x.html
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/release-notes.html
---

# Elastic APM .NET Agent release notes [elastic-apm-net-agent-release-notes]

Review the changes, fixes, and more in each version of Elastic APM .NET Agent.

To check for security updates, go to [Security announcements for the Elastic stack](https://discuss.elastic.co/c/announcements/security-announcements/31).

% Release notes includes only features, enhancements, and fixes. Add breaking changes, deprecations, and known issues to the applicable release notes sections.

% version.next [elastic-apm-net-agent-versionext-release-notes]
% **Release date:** Month day, year

% ### Features and enhancements [elastic-apm-net-agent-versionext-features-enhancements]

% ### Fixes [elastic-apm-net-agent-versionext-fixes]

## 1.31.0 [elastic-apm-net-agent-1310-release-notes]
**Release date:** December 2, 2024

### Features and enhancements [elastic-apm-net-agent-1310-features-enhancements]
* Phase one logger optimisations [#2503](https://github.com/elastic/apm-agent-dotnet/pull/2503)

### Fixes [elastic-apm-net-agent-1310-fixes]
* Fixes and enhancements for Azure Functions [#2505](https://github.com/elastic/apm-agent-dotnet/pull/2505)
* Azure Function service name logic [#2508](https://github.com/elastic/apm-agent-dotnet/pull/2508)

## 1.31.1 [elastic-apm-net-agent-1311-release-notes]
**Release date:** November 18, 2024

### Fixes [elastic-apm-net-agent-1311-fixes]
* Remove netcoreapp2.0 from Elastic.Apm.Profiler.Managed.Loader [#2471](https://github.com/elastic/apm-agent-dotnet/pull/2471)
* Fix span linking for Azure ServiceBus [#2474](https://github.com/elastic/apm-agent-dotnet/pull/2474)
* Support K8S_ATTACH environment variable for activation [#2482](https://github.com/elastic/apm-agent-dotnet/pull/2482)

## 1.31.0 [elastic-apm-net-agent-1310-release-notes]
**Release date:** October 11, 2024

### Fixes [elastic-apm-net-agent-1310-fixes]
* Improve OTel bridge compatibility with existing Azure instrumentation [#2455](https://github.com/elastic/apm-agent-dotnet/pull/2455)
* Revert skipping of System.Web to fix profiler-based installation [#2457](https://github.com/elastic/apm-agent-dotnet/pull/2457)
* Limit attribute count and truncate values in ElasticActivityListener [#2461](https://github.com/elastic/apm-agent-dotnet/pull/2461)
* Add IntakeResponse deserialization for detailed error logging [#2460](https://github.com/elastic/apm-agent-dotnet/pull/2460)

## 1.28.6 [elastic-apm-net-agent-1286-release-notes]
**Release date:** September 11, 2024

### Fixes [elastic-apm-net-agent-1286-fixes]
* Hard exclude several system processes from being auto instrumented [#2431](https://github.com/elastic/apm-agent-dotnet/pull/2431)
* Disabling the agent should not try to enqueue events, now a NOOP [#2436](https://github.com/elastic/apm-agent-dotnet/pull/2436)

## 1.28.5 [elastic-apm-net-agent-1285-release-notes]
**Release date:** August 28, 2024

### Fixes [elastic-apm-net-agent-1285-fixes]
* Relax ECS container ID regex [#2430](https://github.com/elastic/apm-agent-dotnet/pull/2430)

## 1.28.4 [elastic-apm-net-agent-1285-release-notes]
**Release date:** August 19, 2024

### Fixes [elastic-apm-net-agent-1284-fixes]
* Ensure we capture baggage when capturing Errors during unsampled transactions [#2427](https://github.com/elastic/apm-agent-dotnet/pull/2427)
*  Ensure safer access to System.Web.Security.Roles [#2425](https://github.com/elastic/apm-agent-dotnet/pull/2425)
*  Fix a bug that prevented the addition of filters to payloadsenders [#2426](https://github.com/elastic/apm-agent-dotnet/pull/2426)
* SetAgentActivationMethod throws CultureNotFoundException in global-invariant mode [#2423](https://github.com/elastic/apm-agent-dotnet/pull/2423)

## 1.28.3 [elastic-apm-net-agent-1285-release-notes]
**Release date:** August 15, 2024

### Fixes [elastic-apm-net-agent-1283-fixes]
* Update to latest `MongoDB.Driver.Core` to address a breaking change [#2419](https://github.com/elastic/apm-agent-dotnet/pull/2419)
* Adding filters should not force initialization of Agent [#2418](https://github.com/elastic/apm-agent-dotnet/pull/2418)

## 1.28.2 [elastic-apm-net-agent-1282-release-notes]
**Release date:** August 14, 2024

### Fixes [elastic-apm-net-agent-1282-fixes]
* Fixed check for SqlRoleProvider under AspNet Identity 2 [#2415](https://github.com/elastic/apm-agent-dotnet/pull/2415)

## 1.28.1 [elastic-apm-net-agent-1281-release-notes]
**Release date:** August 12, 2024

### Features and enhancements [elastic-apm-net-agent-1281-features-enhancements]
* Global file logging, making it easier to diagnose the agent no matter the deployment type [#2371](https://github.com/elastic/apm-agent-dotnet/pull/2371)

### Fixes [elastic-apm-net-agent-1281-fixes]
* Skip instrumentation of System.Web to prevent rare double configuration initialization issue [#2389](https://github.com/elastic/apm-agent-dotnet/pull/2411)

## 1.28.0 [elastic-apm-net-agent-1289-release-notes]
**Release date:** July 3, 2024

### Fixes [elastic-apm-net-agent-1280-fixes]
* Fix Linux build dependency of glibc [#2389](https://github.com/elastic/apm-agent-dotnet/pull/2389)

## 1.27.3 [elastic-apm-net-agent-1273-release-notes]
**Release date:** June 18, 2024

### Fixes [elastic-apm-net-agent-1273-fixes]
* Release Automation fix [#2380](https://github.com/elastic/apm-agent-dotnet/pull/2380)

## 1.27.2 [elastic-apm-net-agent-1272-release-notes]
**Release date:** June 18, 2024

### Fixes [elastic-apm-net-agent-1272-fixes]
* Clean up dependency graph for .NET core installations [#2308](https://github.com/elastic/apm-agent-dotnet/pull/2308)
* Open Telemetry Bridge should only log when enabled [#2356](https://github.com/elastic/apm-agent-dotnet/pull/2356)
* Bump Microsoft.AspNetCore.Http dep to 2.1.22 [#2166](https://github.com/elastic/apm-agent-dotnet/pull/2166)
* Fix message format for logging in managed profiler [#2350](https://github.com/elastic/apm-agent-dotnet/pull/2350)
* Only mark bodies as redacted if explicitly configured to do so [#2225](https://github.com/elastic/apm-agent-dotnet/pull/2225)
* Do not read claims from SqlRoleProvider under classic ASP.NET [#2377](https://github.com/elastic/apm-agent-dotnet/pull/2377)

## 1.27.1 [elastic-apm-net-agent-1271-release-notes]
**Release date:** May 16, 2024

### Fixes [elastic-apm-net-agent-1271-fixes]
* Remove invalid profiler method integrations [#2349](https://github.com/elastic/apm-agent-dotnet/pull/2349)

## 1.27.0 [elastic-apm-net-agent-1270-release-notes]
**Release date:** April 30, 2024

### Features and enhancements [elastic-apm-net-agent-1270-features-enhancements]
* Add `IServiceCollection` extension methods to register ApmAgent [#2331](https://github.com/elastic/apm-agent-dotnet/pull/2331)
* Add support for `transaction_name_groups` and `use_path_as_transaction_name` [#2326](https://github.com/elastic/apm-agent-dotnet/pull/2326)


* Fix race condition on Add in redis profiler [#2303](https://github.com/elastic/apm-agent-dotnet/pull/2303)
* Further logging refinements in ElasticApmModule [#2299](https://github.com/elastic/apm-agent-dotnet/pull/2299)
* Update to .NET 8 SDK [#2304](https://github.com/elastic/apm-agent-dotnet/pull/2304)
* Update troubleshooting section of docs [#2302](https://github.com/elastic/apm-agent-dotnet/pull/2302)
* Fix bug when handling of multiple cookie entries with the same name [#2310](https://github.com/elastic/apm-agent-dotnet/pull/2310)
* Clarify logging behaviour in troubleshooting doc [#2314](https://github.com/elastic/apm-agent-dotnet/pull/2314)
* Fix agent-zip for 5.0.0 [#2336](https://github.com/elastic/apm-agent-dotnet/pull/2336)

## 1.26.0 [elastic-apm-net-agent-1260-release-notes]
**Release date:** February 20, 2024
This release introduces more thorough sanitization of request/response cookies to align with the APM spec. The incoming `Cookie` is now redacted by default. The cookies it contains are extracted and sanitized according to the `SanitizeFieldNames` configuration. Response headers are now correctly sanitized, including the `Set-Cookie` header. This is a behaviour change!

### Features and enhancements [elastic-apm-net-agent-1260-features-enhancements]
* Fully implement the sanitization spec for request/response headers [#2290](https://github.com/elastic/apm-agent-dotnet/pull/2290)
* Include process information in metadata stanza when emitting events to apm-server [#2272](https://github.com/elastic/apm-agent-dotnet/pull/2272)
* Switch to Licence expression rather than file [#2264](https://github.com/elastic/apm-agent-dotnet/pull/2264)

### Fixes [elastic-apm-net-agent-1271-fixes]
* Cleanup of packages thanks to Framework reference [#2267](https://github.com/elastic/apm-agent-dotnet/pull/2267)
* Limit memory usage when capturing SOAP request bodies [#2274](https://github.com/elastic/apm-agent-dotnet/pull/2274)
* Address a few synchronization issues in the codebase [#2276](https://github.com/elastic/apm-agent-dotnet/pull/2276)
* Truncate unknown keys logging from central config parser [#2277](https://github.com/elastic/apm-agent-dotnet/pull/2277)
* Ensure exposed default constants are readonly [#2278](https://github.com/elastic/apm-agent-dotnet/pull/2278)
* Fix duplicate key errors on dropped span stats update [#2283](https://github.com/elastic/apm-agent-dotnet/pull/2283)
* Remove RegexConverter, not used in serialization from and to apm-server [#2279](https://github.com/elastic/apm-agent-dotnet/pull/2279)
* Cleanup some dead code and one instance of null propagation in tooling NOT userfacing code
[#2280](https://github.com/elastic/apm-agent-dotnet/pull/2280)

## 1.25.3 [elastic-apm-net-agent-1253-release-notes]
**Release date:** January 8, 2024

### Fixes [elastic-apm-net-agent-1253-fixes]
* TagObjects not guaranteed to be unique [#2241](https://github.com/elastic/apm-agent-dotnet/pull/2241)
* Move logging caching over to ConditionalWeaktable [#2242](https://github.com/elastic/apm-agent-dotnet/pull/2242)
* Add additional logging to outgoing http call propagation [#2247](https://github.com/elastic/apm-agent-dotnet/pull/2247)
* Address multiple structured logging violations [#2249](https://github.com/elastic/apm-agent-dotnet/pull/2249)
* Update MongoDB drivers to 2.19.0 [#2245](https://github.com/elastic/apm-agent-dotnet/pull/2245)

## 1.25.2 [elastic-apm-net-agent-1252-release-notes]
**Release date:** December 13, 2023
This release fixes a bug in `Elastic.Apm.AspNetCore` when using ` UseElasticApm()` not correctly setting status codes. The bug was not present in the more commonly used `Elastic.Apm.NetCoreAll` since it uses a `DiagnosticListener` approach. With this release we ensure both packages use the exact same `DiagnosticListener` mechanism to instrument ASP.NET Core.

### Fixes [elastic-apm-net-agent-1252-fixes]
* Remove ApmMiddleWare, only use DiagnosticSource listener for ASP.NET Core [#2213](https://github.com/elastic/apm-agent-dotnet/pull/2213)
* Move StartupHooks over to netstandard2.0 [#2239](https://github.com/elastic/apm-agent-dotnet/pull/2239)

## 1.25.1 [elastic-apm-net-agent-1251-release-notes]
**Release date:** November 21, 2023

### Fixes [elastic-apm-net-agent-1251-fixes]
* Prevent server certificate callback runtime exception [#2213](https://github.com/elastic/apm-agent-dotnet/pull/2213)
* Fix duration.sum.us value in JSON [#2219](https://github.com/elastic/apm-agent-dotnet/pull/2219)
* Return total memory when limit is max value [#2214](https://github.com/elastic/apm-agent-dotnet/pull/2214)
* Ensure baggage gets copied with baggage prefix by [#2220](https://github.com/elastic/apm-agent-dotnet/pull/2220)

## 1.25.0 [elastic-apm-net-agent-1250-release-notes]
**Release date:** October 19, 2023

### Features and enhancements [elastic-apm-net-agent-1260-features-enhancements]
* Support OnExecuteRequestStep available in new .NET versions for IIS modules [#2196](https://github.com/elastic/apm-agent-dotnet/pull/2196)

## 1.24.0 [elastic-apm-net-agent-1240-release-notes]
**Release date:** September 20, 2023

### Features and enhancements [elastic-apm-net-agent-1240-features-enhancements]
* Enable OpenTelemetryBridge by default [#2140](https://github.com/elastic/apm-agent-dotnet/pull/2140)
* Update and optimise OTel bridge [#2157](https://github.com/elastic/apm-agent-dotnet/pull/2157)
* Add Baggage support [#2147](https://github.com/elastic/apm-agent-dotnet/pull/2147)
* Trace in-process Azure Functions [#2160](https://github.com/elastic/apm-agent-dotnet/pull/2160)
* Internalize SqlClient Instrumentation [#2165](https://github.com/elastic/apm-agent-dotnet/pull/2165)

### Fixes [elastic-apm-net-agent-1240-fixes]
* Ensure OpenTelemetryBridge respects Agents sampling decisions [#2170](https://github.com/elastic/apm-agent-dotnet/pull/2170)
* DroppedSpanStats: do not flatten duration [#2178](https://github.com/elastic/apm-agent-dotnet/pull/2178)
* Change *cloud.project.id* for GCP metadata to be the *project-id* [#2180](https://github.com/elastic/apm-agent-dotnet/pull/2180)
* Handle SqlExceptions when accessing user claims [#2182](https://github.com/elastic/apm-agent-dotnet/pull/2182)

## 1.23.0 [elastic-apm-net-agent-1230-release-notes]
**Release date:** August 8, 2023

### Features and enhancements [elastic-apm-net-agent-1230-features-enhancements]
* .NET Full Framework now always loads configuration from web or app.config [#2069](https://github.com/elastic/apm-agent-dotnet/pull/2069)
* Add Npgsql 7.x support to profiler [#2103](https://github.com/elastic/apm-agent-dotnet/pull/2103)
* Backend dependencies granularity for NoSQL and Messaging [#2104](https://github.com/elastic/apm-agent-dotnet/pull/2104)
* Send domain name when detected [#2067](https://github.com/elastic/apm-agent-dotnet/pull/2067)
* Log when we detect LegacyAspNetSynchronizationContext [#2136](https://github.com/elastic/apm-agent-dotnet/pull/2136)

### Fixes [elastic-apm-net-agent-1230-fixes]
* Fix to not send start stack trace when below configured duration [#2126](https://github.com/elastic/apm-agent-dotnet/pull/2126)
* Agent.Configuration now always points to ConfigurationStore’s configuration [#2109](https://github.com/elastic/apm-agent-dotnet/pull/2109)
* Reduce logging noise for stack frame capturing [#2142](https://github.com/elastic/apm-agent-dotnet/pull/2142)
* Move Redis Profiler registration to ConditionalWeakTable [#2148](https://github.com/elastic/apm-agent-dotnet/pull/2148)

## 1.22.0 [elastic-apm-net-agent-1220-release-notes]
**Release date:** April 28, 2023

### Features and enhancements [elastic-apm-net-agent-1230-features-enhancements]
* Enable listening to `Microsoft.Data.SqlClient.EventSource` on .NET full framework [#2050](https://github.com/elastic/apm-agent-dotnet/pull/2050)

### Fixes [elastic-apm-net-agent-1230-fixes]
* Eager load APM configuration [#2054](https://github.com/elastic/apm-agent-dotnet/pull/2054)
* Increase logging of profiler if expected rejit target is not found [#2049](https://github.com/elastic/apm-agent-dotnet/pull/2049)

## 1.21.0 [elastic-apm-net-agent-1210-release-notes]
**Release date:** April 5, 2023

### Fixes [elastic-apm-net-agent-1210-fixes]
* Prevent sending activation_method in metadata for 8.7.0 [#2041](https://github.com/elastic/apm-agent-dotnet/pull/2041)

## 1.20.0 [elastic-apm-net-agent-1200-release-notes]
**Release date:** February 27, 2023

### Features and enhancements [elastic-apm-net-agent-1200-features-enhancements]
* Support for Azure Functions through new `Elastic.Apm.Azure.Functions` nuget package [#1981](https://github.com/elastic/apm-agent-dotnet/pull/1981)
* Support new Elasticsearch Client: `Elastic.Clients.Elasticsearch` [#1935](https://github.com/elastic/apm-agent-dotnet/pull/1935)
* Suppport latest version of Microsoft.Data.SqlClient [#1988](https://github.com/elastic/apm-agent-dotnet/pull/1988)
* Support latest version OracleManagedDataAccess [#1988](https://github.com/elastic/apm-agent-dotnet/pull/1968)
* Loose MSVC redistributable as requirement for the profiler [#1983](https://github.com/elastic/apm-agent-dotnet/pull/1983)
* Add support for sending agent activation method to the server [#1983](https://github.com/elastic/apm-agent-dotnet/pull/1976)

### Fixes [elastic-apm-net-agent-1200-fixes]
* NullReferenceException in span compression [#1999](https://github.com/elastic/apm-agent-dotnet/pull/1999)
* Improve profiler logging by always enabling agent logging too [#1970](https://github.com/elastic/apm-agent-dotnet/pull/1970)
* Normalize OpenTelemetry Bridge config section to `OpenTelemetryBridgeEnabled` [#1972](https://github.com/elastic/apm-agent-dotnet/pull/1972)
* Try to enable TLS 1.2 in all scenarios [#1926](https://github.com/elastic/apm-agent-dotnet/pull/1926)
* OTel bridge span’s destination service may contain null resource [#1964](https://github.com/elastic/apm-agent-dotnet/pull/1964)
* AppSettings ElasticApm:Enabled is not fully honored in ASP.NET Classic [#1961](https://github.com/elastic/apm-agent-dotnet/pull/1961)

## 1.19.0 [elastic-apm-net-agent-1190-release-notes]
**Release date:** December 5, 2022

### Features and enhancements [elastic-apm-net-agent-1190-features-enhancements]
* Improve handling of multiple agent initialization [#1867](https://github.com/elastic/apm-agent-dotnet/pull/1867)
* Enable CloudMetadataProvider on Azure Functions [#1877](https://github.com/elastic/apm-agent-dotnet/pull/1877)
* CentralConfig: handle MaxAge header with less than 5 sec according to spec [#1831](https://github.com/elastic/apm-agent-dotnet/issues/1831) and [#1892](https://github.com/elastic/apm-agent-dotnet/pull/1892)
* Add basic agent logging preamble [#1897](https://github.com/elastic/apm-agent-dotnet/pull/1897)
* Publish docker image with agent [#1665](https://github.com/elastic/apm-agent-dotnet/issues/1665) and [#1907](https://github.com/elastic/apm-agent-dotnet/pull/1907)
* Add .NET 7 support [#1860](https://github.com/elastic/apm-agent-dotnet/issues/1860) and [#1917](https://github.com/elastic/apm-agent-dotnet/pull/1917)
* Improve SOAP action parsing [#1930](https://github.com/elastic/apm-agent-dotnet/pull/1930)

### Fixes [elastic-apm-net-agent-1190-fixes]
* Fix transaction trace id not aligned when transaction is created from OTel bridge without parent [#1881](https://github.com/elastic/apm-agent-dotnet/issues/1881) and [#1882](https://github.com/elastic/apm-agent-dotnet/pull/1882)
* Avoid NRE during startup hook init [#1904](https://github.com/elastic/apm-agent-dotnet/issues/1904) and [#1905](https://github.com/elastic/apm-agent-dotnet/pull/1905)
* Avoid panic in file-logging setup [#1918](https://github.com/elastic/apm-agent-dotnet/issues/1918) and [#1927](https://github.com/elastic/apm-agent-dotnet/pull/1927)
* Use Span timing instead of cumulative SqlCommand statistics [#1869](https://github.com/elastic/apm-agent-dotnet/issues/1869) and [#1922](https://github.com/elastic/apm-agent-dotnet/pull/1922)
* Enable DOTNET_STARTUP_HOOKS for .NET 7 [#1900](https://github.com/elastic/apm-agent-dotnet/issues/1900) and [#1933](https://github.com/elastic/apm-agent-dotnet/pull/1933)

## 1.18.0 [elastic-apm-net-agent-1180-release-notes]
**Release date:** October 13, 2022

### Features and enhancements [elastic-apm-net-agent-1180-features-enhancements]
* Profiler based agent is now GA
* Capture request body in ASP.NET Full Framework [#379](https://github.com/elastic/apm-agent-dotnet/issues/379) and [#1806](https://github.com/elastic/apm-agent-dotnet/pull/1806)
* `UseWindowsCredentials`: new configuration to force the agent to use the credentials of the authenticated Windows user when events are sent to the APM Server [#1825](https://github.com/elastic/apm-agent-dotnet/issues/1825) and [#1832](https://github.com/elastic/apm-agent-dotnet/pull/1832)

### Fixes [elastic-apm-net-agent-1180-fixes]
* Fix incorrect transaction name in ASP.NET Web Api [#1637](https://github.com/elastic/apm-agent-dotnet/issues/1645) and [#1800](https://github.com/elastic/apm-agent-dotnet/pull/1800)
* Fix potential NullReferenceException in TraceContinuationStrategy implementation [#1802](https://github.com/elastic/apm-agent-dotnet/issues/1802) and [#1803](https://github.com/elastic/apm-agent-dotnet/pull/1803) and [#1804](https://github.com/elastic/apm-agent-dotnet/pull/1804)
* Fix container ID parsing in AWS ECS/Fargate environments [#1779](https://github.com/elastic/apm-agent-dotnet/issues/1779) and [#1780](https://github.com/elastic/apm-agent-dotnet/pull/1780)
* Use correct default value for ExitSpanMinDuration [#1789](https://github.com/elastic/apm-agent-dotnet/issues/1789) and [#1814](https://github.com/elastic/apm-agent-dotnet/pull/1814)
* Fixed crashes on some SOAP 1.2 requests when using GetBufferedInputStream [#1759](https://github.com/elastic/apm-agent-dotnet/issues/1759) and [#1811](https://github.com/elastic/apm-agent-dotnet/pull/1811)
* Group MetricSets in BreakdownMetricsProvider [#1678](https://github.com/elastic/apm-agent-dotnet/issues/1678) and [#1816](https://github.com/elastic/apm-agent-dotnet/pull/1816)

## 1.17.0 [elastic-apm-net-agent-1170-release-notes]
**Release date:** August 28, 2022

### Features and enhancements [elastic-apm-net-agent-1170-features-enhancements]
* Introduce the `TraceContinuationStrategy` config [#1637](https://github.com/elastic/apm-agent-dotnet/issues/1637) and [#1739](https://github.com/elastic/apm-agent-dotnet/pull/1739)
* Span Links with Azure ServiceBus [#1638](https://github.com/elastic/apm-agent-dotnet/issues/1638) and [#1749](https://github.com/elastic/apm-agent-dotnet/pull/1749)
* Improve db granularity [#1664](https://github.com/elastic/apm-agent-dotnet/issues/1664) and [#1765](https://github.com/elastic/apm-agent-dotnet/pull/1765)
* Add config option `span_stack_trace_min_duration` [#1529](https://github.com/elastic/apm-agent-dotnet/issues/1529) and [#1795](https://github.com/elastic/apm-agent-dotnet/pull/1795)

### Fixes [elastic-apm-net-agent-1170-fixes]
* Fix default for the `ApplicationNamespaces` config [#1746](https://github.com/elastic/apm-agent-dotnet/pull/1746)
* Flow SynchronizationContext across public API calls [#1660](https://github.com/elastic/apm-agent-dotnet/issues/1660) and [#1755](https://github.com/elastic/apm-agent-dotnet/pull/1755)
* PayloadSender threading improvements [#1571](https://github.com/elastic/apm-agent-dotnet/issues/1571) and [#1753](https://github.com/elastic/apm-agent-dotnet/pull/1753)
* Include Accept header on APM server info call (caused errors when reading APM Server info) [#1624](https://github.com/elastic/apm-agent-dotnet/issues/1624) and [#1773](https://github.com/elastic/apm-agent-dotnet/pull/1773)
* Significantly improved the performance of database query parsing [#1763](https://github.com/elastic/apm-agent-dotnet/issues/1763) and [#1781](https://github.com/elastic/apm-agent-dotnet/pull/1781)
* Fix FillApmServerInfo : Invalid ElasticApm_ApiKey throws Exception [#1735](https://github.com/elastic/apm-agent-dotnet/issues/1735) and [#1787](https://github.com/elastic/apm-agent-dotnet/pull/1787)

## 1.16.1 [elastic-apm-net-agent-1161-release-notes]
**Release date:** June 15, 2022

### Features and enhancements [elastic-apm-net-agent-1161-features-enhancements]
* Improved logging around fetching central configuration [#1626](https://github.com/elastic/apm-agent-dotnet/issues/1626) and [#1732](https://github.com/elastic/apm-agent-dotnet/pull/1732)

### Fixes [elastic-apm-net-agent-1161-fixes]
* Crash during assembly loading with the profiler based agent [#1705](https://github.com/elastic/apm-agent-dotnet/issues/1705) and [#1710](https://github.com/elastic/apm-agent-dotnet/pull/1710)
* Handling RouteData with `null` in legacy ASP.NET Core 2.2 apps [#1729](https://github.com/elastic/apm-agent-dotnet/issues/1729)

## 1.16.0 [elastic-apm-net-agent-1160-release-notes]
**Release date:** June 2, 2022

### Features and enhancements [elastic-apm-net-agent-1160-features-enhancements]
* Automatic capturing of incoming HTTP Requests on ASP.NET Core with the Profiler based agent [#1610](https://github.com/elastic/apm-agent-dotnet/issues/1610) and [#1726](https://github.com/elastic/apm-agent-dotnet/pull/1726)

### Fixes [elastic-apm-net-agent-1160-fixes]
* By disabling `system.cpu.total.norm.pct`, the agent won’t create any instance of the `PerformanceCounter` type (workaround for issue: [#1724](https://github.com/elastic/apm-agent-dotnet/issues/1724) and [#1725](https://github.com/elastic/apm-agent-dotnet/pull/1725)
* Transaction names for incoming HTTP requests returning 404 but matching a valid route, will include the URL path instead of using `unknown route` [#1715](https://github.com/elastic/apm-agent-dotnet/issues/1715) and [#1723](https://github.com/elastic/apm-agent-dotnet/pull/1723)

## 1.15.0 [elastic-apm-net-agent-1150-release-notes]
**Release date:** May 12, 2022

### Features and enhancements [elastic-apm-net-agent-1160-features-enhancements]
* Improved database span names based on parsed SQL statements [#242](https://github.com/elastic/apm-agent-dotnet/issues/242) and [#1657](https://github.com/elastic/apm-agent-dotnet/pull/1657)

### Fixes [elastic-apm-net-agent-1150-fixes]
* Dedicated working loop thread for sending APM events [#1571](https://github.com/elastic/apm-agent-dotnet/issues/1571) and [#1670](https://github.com/elastic/apm-agent-dotnet/pull/1670)
* Fixed span type for MongoDB - with this a MongoDB logo will show up on the service map [#1677](https://github.com/elastic/apm-agent-dotnet/pull/1677)
* InvalidCastException in `AspNetCoreDiagnosticListener` [#1674](https://github.com/elastic/apm-agent-dotnet/pull/1674)
* MVC: handling `area:null` when creating transaction name based on routing [#1683](https://github.com/elastic/apm-agent-dotnet/pull/1683)
* Handle missing `.Stop` events in `AspNetCoreDiagnosticListener` [#1676](https://github.com/elastic/apm-agent-dotnet/issues/1676) and [#1685](https://github.com/elastic/apm-agent-dotnet/pull/1685)

## 1.14.1 [elastic-apm-net-agent-1141-release-notes]
**Release date:** March 10, 2022

### Fixes [elastic-apm-net-agent-1150-fixes]
* Make sure events are sent after APM Server timeout [#1630](https://github.com/elastic/apm-agent-dotnet/pull/1630) and [#1634](https://github.com/elastic/apm-agent-dotnet/pull/1634)
* Error on composite span validation [#1631](https://github.com/elastic/apm-agent-dotnet/issues/1631) and [#1639](https://github.com/elastic/apm-agent-dotnet/pull/1639)
* OpenTelemetry (Activity) bridge - APM Server version check [#1648](https://github.com/elastic/apm-agent-dotnet/pull/1648)

## 1.14.0 [elastic-apm-net-agent-1140-release-notes]
**Release date:** February 9, 2022

### Features and enhancements [elastic-apm-net-agent-1140-features-enhancements]
* Span compression and dropping fast exit spans. New settings: `ExitSpanMinDuration`, `SpanCompressionEnabled`, `SpanCompressionExactMatchMaxDuration`, `SpanCompressionSameKindMaxDuration` [#1329](https://github.com/elastic/apm-agent-dotnet/issues/1329) and [#1475](https://github.com/elastic/apm-agent-dotnet/issues/1475) and [#1620](https://github.com/elastic/apm-agent-dotnet/pull/1620)
* NpgSql 6.x support [#1602](https://github.com/elastic/apm-agent-dotnet/issues/1602) and [#1611](https://github.com/elastic/apm-agent-dotnet/pull/1611)
* Capture transaction name on errors [#1574](https://github.com/elastic/apm-agent-dotnet/issues/1574) and [#1589](https://github.com/elastic/apm-agent-dotnet/pull/1589)

### Fixes [elastic-apm-net-agent-1140-fixes]
* .NET 6 support with startup hook [#1590](https://github.com/elastic/apm-agent-dotnet/issues/1590) and [#1603](https://github.com/elastic/apm-agent-dotnet/pull/1603)

## 1.13.0 [elastic-apm-net-agent-1130-release-notes]
**Release date:** January 12, 2022

### Features and enhancements [elastic-apm-net-agent-1140-features-enhancements]
* OpenTelemetry Bridge - integration with `System.Diagnostics.Activity` - Beta [#1521](https://github.com/elastic/apm-agent-dotnet/issues/1521) and [#1498](https://github.com/elastic/apm-agent-dotnet/pull/1498)

## 1.12.1 [elastic-apm-net-agent-1121-release-notes]

### Fixes [elastic-apm-net-agent-1121-fixes]
* Failed sending event error with missing span.context.destination.service.name required field on older APM Servers [#1563](https://github.com/elastic/apm-agent-dotnet/issues/1563) and [#1564](https://github.com/elastic/apm-agent-dotnet/pull/1564) )

## 1.12.0 [elastic-apm-net-agent-1120-release-notes]

### Features and enhancements [elastic-apm-net-agent-1120-features-enhancements]
* Implement Dropped span statistics [#1511](https://github.com/elastic/apm-agent-dotnet/pull/1511)
* Ignore duplicate Diagnostic listener subscriptions [#1119](https://github.com/elastic/apm-agent-dotnet/issues/1119) and [#1515](https://github.com/elastic/apm-agent-dotnet/pull/1515)
* Implement User-Agent spec for .NET agent [#1525](https://github.com/elastic/apm-agent-dotnet/pull/1525) and [#1518](https://github.com/elastic/apm-agent-dotnet/pull/1518)
* Add message related properties to transactions and spans [#1512](https://github.com/elastic/apm-agent-dotnet/issues/1512)
* Add profiler auto instrumentation [#1522](https://github.com/elastic/apm-agent-dotnet/issues/1522) and [#1534](https://github.com/elastic/apm-agent-dotnet/pull/1534)
* Add profiler auto instrumentation for RabbitMQ [#1223](https://github.com/elastic/apm-agent-dotnet/issues/1223) and [#1548](https://github.com/elastic/apm-agent-dotnet/pull/1548)
* Platform detection: Handle .NET 6 [#1513](https://github.com/elastic/apm-agent-dotnet/issues/1513) and [#1528](https://github.com/elastic/apm-agent-dotnet/pull/1528)
* Remove use of Socket.Encrypted to determine secure [#1492](https://github.com/elastic/apm-agent-dotnet/pull/1492)
* Auto-infer destination.service.resource and adapt public API [#1330](https://github.com/elastic/apm-agent-dotnet/issues/1330) and [#1520](https://github.com/elastic/apm-agent-dotnet/pull/1520)
* Stop recording transaction metrics [#1523](https://github.com/elastic/apm-agent-dotnet/issues/1523) and [#1540](https://github.com/elastic/apm-agent-dotnet/pull/1540)

### Fixes [elastic-apm-net-agent-1120-fixes]
* Capture spans for new Azure Storage SDKs [#1352](https://github.com/elastic/apm-agent-dotnet/issues/1352) and [#1484](https://github.com/elastic/apm-agent-dotnet/pull/1484)
* Use Environment.MachineName to get HostName [#1504](https://github.com/elastic/apm-agent-dotnet/issues/1504) and [#1509](https://github.com/elastic/apm-agent-dotnet/pull/1509)
* Check context is not null when sanitizing error request headers [#1503](https://github.com/elastic/apm-agent-dotnet/issues/1503) and [#1510](https://github.com/elastic/apm-agent-dotnet/pull/1510)
* Improve Performance counter handling for metrics on Windows [#1505](https://github.com/elastic/apm-agent-dotnet/issues/1505) and [#1536](https://github.com/elastic/apm-agent-dotnet/pull/1536)
* Collect .NET Framework GC metrics only when filtering supported [#1346](https://github.com/elastic/apm-agent-dotnet/issues/1346) and [#1538](https://github.com/elastic/apm-agent-dotnet/pull/1538)
* Handle enabled/recording=false configuration when capturing errors [#1557](https://github.com/elastic/apm-agent-dotnet/pull/1557)

## 1.11.1 [elastic-apm-net-agent-1111-release-notes]

### Features and enhancements [elastic-apm-net-agent-1111-features-enhancements]
* Serialize to writer directly [#1354](https://github.com/elastic/apm-agent-dotnet/pull/1354)
* Better logging in PayloadSenderV2 on task cancellation [#1356](https://github.com/elastic/apm-agent-dotnet/pull/1356)
* Propagate Trace context in exit spans [#1350](https://github.com/elastic/apm-agent-dotnet/issues/1350), [#1344](https://github.com/elastic/apm-agent-dotnet/issues/1344), and [#1358](https://github.com/elastic/apm-agent-dotnet/pull/1358)
* Get Command and Key for StackExchange.Redis spans [#1364](https://github.com/elastic/apm-agent-dotnet/issues/1364) and [#1374](https://github.com/elastic/apm-agent-dotnet/pull/1374)
* Add CosmosDB integration to NetCoreAll [#1474](https://github.com/elastic/apm-agent-dotnet/pull/1474)
* Use 10K limit for CaptureBody similar to the Java agent [#1359](https://github.com/elastic/apm-agent-dotnet/issues/1359) and [#1368](https://github.com/elastic/apm-agent-dotnet/pull/1368)

### Fixes [elastic-apm-net-agent-1111-fixes]
* Unset parentId if TraceContextIgnoreSampledFalse is active [#1362](https://github.com/elastic/apm-agent-dotnet/pull/1362)
* Make sure BreakdownMetricsProvider prints 1K warning only once per collection [#1361](https://github.com/elastic/apm-agent-dotnet/issues/1361) and [#1367](https://github.com/elastic/apm-agent-dotnet/pull/1367)
* Sanitize Central config request URI and headers in logs [#1376](https://github.com/elastic/apm-agent-dotnet/issues/1376) and [#1471](https://github.com/elastic/apm-agent-dotnet/pull/1471)
* Honor Transaction.Outcome set by public API in auto instrumentation [#1349](https://github.com/elastic/apm-agent-dotnet/issues/1349) and [#1472](https://github.com/elastic/apm-agent-dotnet/pull/1472)
* Use Kubernetes pod id determined from cgroup file [#1481](https://github.com/elastic/apm-agent-dotnet/pull/1481)

## 1.11.0 [elastic-apm-net-agent-1110-release-notes]

### Features and enhancements [elastic-apm-net-agent-1110-features-enhancements]
* CosmosDb support [#1154](https://github.com/elastic/apm-agent-dotnet/issues/1154) and [#1342](https://github.com/elastic/apm-agent-dotnet/pull/1342)
* Support "Time spent by span type" (aka Breakdown metrics) [#227](https://github.com/elastic/apm-agent-dotnet/issues/227) and [#1271](https://github.com/elastic/apm-agent-dotnet/pull/1271)
* Prefer W3C traceparent over elastic-apm-traceparent [#1302](https://github.com/elastic/apm-agent-dotnet/pull/1302)
* Add TraceContextIgnoreSampledFalse config setting [#1310](https://github.com/elastic/apm-agent-dotnet/pull/1310)
* Create transactions for Azure Service Bus Processors [#1321](https://github.com/elastic/apm-agent-dotnet/issues/1321) and [#1331](https://github.com/elastic/apm-agent-dotnet/pull/1331)

## 1.10.0 [elastic-apm-net-agent-1100-release-notes]

### Features and enhancements [elastic-apm-net-agent-1100-features-enhancements]
* Add instrumentation for Azure Service Bus [#1157](https://github.com/elastic/apm-agent-dotnet/issues/1157) and [#1225](https://github.com/elastic/apm-agent-dotnet/pull/1225)
* Add Azure storage integration [#1156](https://github.com/elastic/apm-agent-dotnet/issues/1156) and [#1155](https://github.com/elastic/apm-agent-dotnet/issues/1155) and [#1247](https://github.com/elastic/apm-agent-dotnet/pull/1247)
* Internalize `Newtonsoft.Json` - no more dependency on `Newtonsoft.Json` [#1241](https://github.com/elastic/apm-agent-dotnet/pull/1241)
* Internalize `Ben.Demystifier` - no more dependency on `Ben.Demystifier` [#1232](https://github.com/elastic/apm-agent-dotnet/issues/1232) and [#1275](https://github.com/elastic/apm-agent-dotnet/pull/1275)
* Add MongoDb support [#1158](https://github.com/elastic/apm-agent-dotnet/issues/1158) and [#1215](https://github.com/elastic/apm-agent-dotnet/pull/1215)
* Capture inner exceptions [#1267](https://github.com/elastic/apm-agent-dotnet/issues/1267) and [#1277](https://github.com/elastic/apm-agent-dotnet/pull/1277)
* Add configured hostname [#1289](https://github.com/elastic/apm-agent-dotnet/issues/1289) and [#1290](https://github.com/elastic/apm-agent-dotnet/pull/1290)
* Use TraceLogger as default logger in ASP.NET Full Framework [#1263](https://github.com/elastic/apm-agent-dotnet/issues/1263) and [#1288](https://github.com/elastic/apm-agent-dotnet/pull/1288)

### Fixes [elastic-apm-net-agent-1100-fixes]
* Fix issue around setting `Recording` to `false` [#1250](https://github.com/elastic/apm-agent-dotnet/issues/1250) and [#1252](https://github.com/elastic/apm-agent-dotnet/pull/1252)
* ASP.NET: Move error capturing to Error event handler [#1259](https://github.com/elastic/apm-agent-dotnet/pull/1259)
* Use Logger to log exception in AgentComponents initialization [#1254](https://github.com/elastic/apm-agent-dotnet/issues/1254) and [#1305](https://github.com/elastic/apm-agent-dotnet/pull/1305)
* Fix `NullReferenceException` in Elastic.Apm.Extensions.Logging [#1309](https://github.com/elastic/apm-agent-dotnet/issues/1309) and [#1311](https://github.com/elastic/apm-agent-dotnet/pull/1311)

## 1.9.0 [elastic-apm-net-agent-190-release-notes]

### Features and enhancements [elastic-apm-net-agent-190-features-enhancements]
* Add GC time [#922](https://github.com/elastic/apm-agent-dotnet/issues/922) and [#925](https://github.com/elastic/apm-agent-dotnet/pull/925)
* Propagate sample rate through `tracestate` [#1021](https://github.com/elastic/apm-agent-dotnet/issues/1021) and [#1147](https://github.com/elastic/apm-agent-dotnet/pull/1147)

### Fixes [elastic-apm-net-agent-190-fixes]
* Get transaction name from Web API controller route template [#1189](https://github.com/elastic/apm-agent-dotnet/pull/1189)

## 1.8.1 [elastic-apm-net-agent-181-release-notes]

### Features and enhancements [elastic-apm-net-agent-181-features-enhancements]
* Add GC Heap Stats capturing for .NET 5.0 [#1195](https://github.com/elastic/apm-agent-dotnet/issues/1195) and [#1196](https://github.com/elastic/apm-agent-dotnet/pull/1196)

### Fixes [elastic-apm-net-agent-181-fixes]
* Lazily access the agent in ElasticApmProfiler redis integration [#1190](https://github.com/elastic/apm-agent-dotnet/issues/1190) and [#1192](https://github.com/elastic/apm-agent-dotnet/pull/1192)
* Add TargetFramework NET5.0 to Elastic.Apm.AspNetCore and related packages [#1194](https://github.com/elastic/apm-agent-dotnet/issues/1194) and [#1198](https://github.com/elastic/apm-agent-dotnet/pull/1198)

## 1.8.0 [elastic-apm-net-agent-180-release-notes]

### Features and enhancements [elastic-apm-net-agent-180-features-enhancements]
* Add support for capturing redis commands from StackExchange.Redis ([documentation](/reference/setup-stackexchange-redis.md)) [#874](https://github.com/elastic/apm-agent-dotnet/issues/874) and [#1063](https://github.com/elastic/apm-agent-dotnet/pull/1063)
* Introduce `ServerUrl` config - (`ServerUrls` is still working but will be removed in the future) [#1035](https://github.com/elastic/apm-agent-dotnet/issues/1035) and [#1065](https://github.com/elastic/apm-agent-dotnet/pull/1065)
* Support for more k8s cgroup path patterns [#968](https://github.com/elastic/apm-agent-dotnet/issues/968) and [#1048](https://github.com/elastic/apm-agent-dotnet/pull/1048)
* `SanitizeFieldNames` config became changeable though Kibana central configuration [#1082](https://github.com/elastic/apm-agent-dotnet/pull/1082)
* Azure App Service cloud metadata collection [#1083](https://github.com/elastic/apm-agent-dotnet/pull/1083)
* Capture error logs as APM errors from `Microsoft.Extensions.Logging` automatically and extend the Public API to capture custom logs as APM errors [#894](https://github.com/elastic/apm-agent-dotnet/issues/894) and [#1135](https://github.com/elastic/apm-agent-dotnet/pull/1135)
* Support changing log level through Kibana central configuration and support `"off"` level [#970](https://github.com/elastic/apm-agent-dotnet/issues/970) and [#1096](https://github.com/elastic/apm-agent-dotnet/pull/1096)

### Fixes [elastic-apm-net-agent-180-fixes]
* `NullReferenceException` with disabled agent on `Transaction.Custom` [#1080](https://github.com/elastic/apm-agent-dotnet/issues/1080) and [#1081](https://github.com/elastic/apm-agent-dotnet/pull/1081)
* ASP.NET Core, enabled=false in `appsettings.json` does not disable public Agent API [#1077](https://github.com/elastic/apm-agent-dotnet/issues/1077) and [#1078](https://github.com/elastic/apm-agent-dotnet/pull/1078)
* `System.IO.IOException` on ASP.NET Classic [#1113](https://github.com/elastic/apm-agent-dotnet/issues/1113) and [#1115](https://github.com/elastic/apm-agent-dotnet/pull/1115)
* Memory issue with gRPC  [#1116](https://github.com/elastic/apm-agent-dotnet/issues/1116) and [#1118](https://github.com/elastic/apm-agent-dotnet/pull/1118)
* Ensuring ETW sessions are terminated on agent shutdown [#897](https://github.com/elastic/apm-agent-dotnet/issues/897) and [#1124](https://github.com/elastic/apm-agent-dotnet/pull/1124)
* `NullReferenceException` with custom `IConfigurationReader` implementation in `MetricsCollector` [#1109](https://github.com/elastic/apm-agent-dotnet/pull/1109)
* Fixes around zero code change agent setup with `DOTNET_STARTUP_HOOKS` [#1138](https://github.com/elastic/apm-agent-dotnet/pull/1138) and [#1165](https://github.com/elastic/apm-agent-dotnet/pull/1165)
* Access `Request.InputStream` only when SOAP header present [#1113](https://github.com/elastic/apm-agent-dotnet/issues/1113)
 and [#1115](https://github.com/elastic/apm-agent-dotnet/pull/1115)

## 1.7.1 [elastic-apm-net-agent-171-release-notes]

### Features and enhancements [elastic-apm-net-agent-180-features-enhancements]
* Introduce `GetLabel<T>` method on `IExecutionSegment` [#1033](https://github.com/elastic/apm-agent-dotnet/issues/1033) and [#1057](https://github.com/elastic/apm-agent-dotnet/pull/1057)

### Fixes [elastic-apm-net-agent-171-fixes]
* Increased transaction duration due to stack trace capturing [#1039](https://github.com/elastic/apm-agent-dotnet/issues/1039) and [#1052](https://github.com/elastic/apm-agent-dotnet/pull/1052)
* Warning with `Synchronous operations are disallowed` on ASP.NET Core during request body capturing [#1044](https://github.com/elastic/apm-agent-dotnet/issues/1044) and [#1053](https://github.com/elastic/apm-agent-dotnet/pull/1053)
* SqlClient instrumentation on .NET 5 [#1025](https://github.com/elastic/apm-agent-dotnet/issues/1025) and [#1042](https://github.com/elastic/apm-agent-dotnet/pull/1042)
* `UseAllElasticApm` with `IHostBuilder` missing auto instrumentation [#1059](https://github.com/elastic/apm-agent-dotnet/issues/1059) and [#1060](https://github.com/elastic/apm-agent-dotnet/pull/1060)

## 1.7.0 [elastic-apm-net-agent-170-release-notes]

### Features and enhancements [elastic-apm-net-agent-170-features-enhancements]
* Agent loading with zero code change on .NET Core [#71](https://github.com/elastic/apm-agent-dotnet/issues/71) and [#828](https://github.com/elastic/apm-agent-dotnet/pull/828)
* gRPC support [#478](https://github.com/elastic/apm-agent-dotnet/issues/478) and [#969](https://github.com/elastic/apm-agent-dotnet/pull/969)
* Add ability to configure Hostname [#932](https://github.com/elastic/apm-agent-dotnet/issues/932) and [#974](https://github.com/elastic/apm-agent-dotnet/pull/974)
* Add Enabled and Recording configuration #122) and [#997](https://github.com/elastic/apm-agent-dotnet/pull/997)
* Add `FullFrameworkConfigurationReaderType` config to load custom configuration reader on ASP.NET [#912](https://github.com/elastic/apm-agent-dotnet/pull/912)
* Capture User id and email on ASP.NET #540 and [#978](https://github.com/elastic/apm-agent-dotnet/pull/978)
* Support boolean and numeric labels in addition to string labels  [#967](https://github.com/elastic/apm-agent-dotnet/issues/967), [#788](https://github.com/elastic/apm-agent-dotnet/issues/788), [#473](https://github.com/elastic/apm-agent-dotnet/issues/473), [#191](https://github.com/elastic/apm-agent-dotnet/issues/192), [#788](https://github.com/elastic/apm-agent-dotnet/issues/788), [#473](https://github.com/elastic/apm-agent-dotnet/issues/473), [#191](https://github.com/elastic/apm-agent-dotnet/issues/191), and [#982](https://github.com/elastic/apm-agent-dotnet/pull/982)
* Collecting metrics based on cGroup [#937](https://github.com/elastic/apm-agent-dotnet/issues/937) and [#1000](https://github.com/elastic/apm-agent-dotnet/pull/1000)
* `ITransaction.SetService` API to support multiple services in a single process [#1001](https://github.com/elastic/apm-agent-dotnet/issues/1001) and [#1002](https://github.com/elastic/apm-agent-dotnet/pull/1002)
* Collecting cloud metadata (supporting AWS, Azure,  GCP) [#918](https://github.com/elastic/apm-agent-dotnet/issues/918) and [#1003](https://github.com/elastic/apm-agent-dotnet/pull/1003)
* Transaction grouping on ASP.NET [#201](https://github.com/elastic/apm-agent-dotnet/issues/) and [#973](https://github.com/elastic/apm-agent-dotnet/pull/973)
* Entity Framework 6 support on .NET Core [#902](https://github.com/elastic/apm-agent-dotnet/issues/902) and [#913](https://github.com/elastic/apm-agent-dotnet/pull/913)

### Fixes [elastic-apm-net-agent-170-fixes]
* [#992](https://github.com/elastic/apm-agent-dotnet/pull/992) On ASP.NET Core `CurrentTransaction` is null in some cases (issues: [#934](https://github.com/elastic/apm-agent-dotnet/issues/934), [#972](https://github.com/elastic/apm-agent-dotnet/issues/972))
* [#971](https://github.com/elastic/apm-agent-dotnet/pull/971) Avoid double initialization in `HostBuilderExtensions`
* [#999](https://github.com/elastic/apm-agent-dotnet/pull/999) Capture body with large file error (issue: [#960](https://github.com/elastic/apm-agent-dotnet/issues/960))

## 1.6.1 [elastic-apm-net-agent-161-release-notes]

### Fixes [elastic-apm-net-agent-161-fixes]
* Service map: missing connection between .NET services ([#909](https://github.com/elastic/apm-agent-dotnet/pull/909))

## 1.6.0 [elastic-apm-net-agent-160-release-notes]

### Features and enhancements [elastic-apm-net-agent-160-features-enhancements]
* Elasticsearch client instrumentation [#329](https://github.com/elastic/apm-agent-dotnet/pull/329)
* Introducing `Elastic.Apm.Extensions.Hosting` package with an extension method on `IHostBuilder` [#537](https://github.com/elastic/apm-agent-dotnet/pull/537)
* Stack trace improvements: async call stack demystification ([#847](https://github.com/elastic/apm-agent-dotnet/pull/847)) and showing frames from user code for outgoing HTTP calls ([#845](https://github.com/elastic/apm-agent-dotnet/pull/845))
* Making fields on `IError` public [#847](https://github.com/elastic/apm-agent-dotnet/pull/847)
* Service map improvements: [#893](https://github.com/elastic/apm-agent-dotnet/pull/893)

### Fixes [elastic-apm-net-agent-160-fixes]
* Missing traces from the Kibana traces list due to setting `Transaction.ParentId` to an `Activity` [#888](https://github.com/elastic/apm-agent-dotnet/pull/888)
* Exception around runtime detection [#859](https://github.com/elastic/apm-agent-dotnet/pull/859)
* Missing outgoing HTTP calls in .NET Framework applications and causing memory issues [#896](https://github.com/elastic/apm-agent-dotnet/pull/896)

## 1.5.1 [elastic-apm-net-agent-151-release-notes]

### Fixes [elastic-apm-net-agent-151-fixes]
* Memory issue in SqlEventListener [#851](https://github.com/elastic/apm-agent-dotnet/pull/851)

## 1.5.0 [elastic-apm-net-agent-150-release-notes]

### Features and enhancements [elastic-apm-net-agent-150-features-enhancements]
* Auto instrumentation for `SqlClient` ([documentation](/reference/setup-sqlclient.md))
* Introducing Filter API [#792](https://github.com/elastic/apm-agent-dotnet/pull/792) ([documentation](/reference/public-api.md#filter-api))
* Auto-detect culprit for exceptions [#740](https://github.com/elastic/apm-agent-dotnet/pull/740)
* New config settings: `ExcludedNamespaces`, `ApplicationNamespaces` ([documentation](/reference/config-all-options-summary.md))
* Keep `Activity.Current.TraceId` in sync with the Trace ID used by the agent [#800](https://github.com/elastic/apm-agent-dotnet/pull/800)
* Report Kubernetes system metadata [#741](https://github.com/elastic/apm-agent-dotnet/pull/741)

### Fixes [elastic-apm-net-agent-150-fixes]
* Database connection string parsing issue with Oracle [#795](https://github.com/elastic/apm-agent-dotnet/pull/795)

## 1.4.0 [elastic-apm-net-agent-140-release-notes]

### Features and enhancements [elastic-apm-net-agent-140-features-enhancements]
* Introducing `ITransaction.EnsureParentId()` to integrate with RUM in dynamically loaded HTML pages (including page loads in ASP.NET Core) [#771](https://github.com/elastic/apm-agent-dotnet/pull/771)
* New config setting: `ApiKey` [#733](https://github.com/elastic/apm-agent-dotnet/pull/733)

### Fixes [elastic-apm-net-agent-140-fixes]
* Memory issue in .NET Full Framework with default metrics turned on [#750](https://github.com/elastic/apm-agent-dotnet/pull/750)
* Parsing for Oracle connection strings [#749](https://github.com/elastic/apm-agent-dotnet/pull/749)
* `StackOverflowException` when using the `Elastic.Apm.SerilogEnricher` package and the log level is set to `Verbose` [#753](https://github.com/elastic/apm-agent-dotnet/pull/753)

## 1.3.1 [elastic-apm-net-agent-131-release-notes]

### Fixes [elastic-apm-net-agent-131-fixes]
* Fix log spamming issues  [#736](https://github.com/elastic/apm-agent-dotnet/pull/736), [#738](https://github.com/elastic/apm-agent-dotnet/pull/738)
* Fix turning HTTP 415 responses in ASP.NET Core to HTTP 500 when request body capturing is active [#739](https://github.com/elastic/apm-agent-dotnet/pull/739)
* Fix disabling GC metrics collection in case no GC is triggered during the first "5*MetricsInterval" of the process [#745](https://github.com/elastic/apm-agent-dotnet/pull/745)

## 1.3.0 [elastic-apm-net-agent-130-release-notes]

### Features and enhancements [elastic-apm-net-agent-130-features-enhancements]
* New GC metrics: `clr.gc.count`, `clr.gc.gen[X]size`, where [X]: heap generation [#697](https://github.com/elastic/apm-agent-dotnet/pull/697)
* Capturing SOAP action name as part of the transaction name [#683](https://github.com/elastic/apm-agent-dotnet/pull/683)
* New config options: `ServiceNodeName`, `VerifyServerCert`, `DisableMetrics`, `UseElasticTraceparentHeader` ([docs](/reference/config-all-options-summary.md))
* Full [W3C TraceContext](https://www.w3.org/TR/trace-context) support [#717](https://github.com/elastic/apm-agent-dotnet/pull/717)

### Fixes [elastic-apm-net-agent-130-fixes]
* Fix transaction name generation in ASP.NET Core 3.x [#647](https://github.com/elastic/apm-agent-dotnet/pull/647)
* Fix around HTTP request body sanitization [#712](https://github.com/elastic/apm-agent-dotnet/pull/712)

## 1.2.0 [elastic-apm-net-agent-120-release-notes]

### Features and enhancements [elastic-apm-net-agent-120-features-enhancements]
* Entity framework support with Interceptor ([docs](/reference/setup-ef6.md))
* Sanitization of HTTP headers and request body ([docs](/reference/config-core.md#config-sanitize-field-names))
* Central configuration - 2 new configs: `CAPTURE_BODY` and `TRANSACTION_MAX_SPANS`. [#577](https://github.com/elastic/apm-agent-dotnet/pull/577).
* Support for global labels ([docs](/reference/config-core.md#config-global-labels))
* Custom context ([docs](/reference/public-api.md#api-transaction-context))
* Dropping support for ASP.NET Core 2.0 (which is already end of life) ([docs](/reference/supported-technologies.md#supported-web-frameworks))

### Fixes [elastic-apm-net-agent-120-fixes]
* De-dotting labels. [#583](https://github.com/elastic/apm-agent-dotnet/pull/583).
* Request body capturing TypeLoadException in ASP.NET Core 3.0. [#604](https://github.com/elastic/apm-agent-dotnet/pull/604).
* Metrics collection: filtering NaN and Infinity. [#589](https://github.com/elastic/apm-agent-dotnet/pull/589).

## 1.1.2 [elastic-apm-net-agent-112-release-notes]

### Fixes [elastic-apm-net-agent-112-fixes]
* Capturing request body with ASP.NET Core erased the body in some scenarios [#539](https://github.com/elastic/apm-agent-dotnet/pull/539).
* Integration with Serilog caused missing logs and diagnostic traces with `NullReferenceException` [#544](https://github.com/elastic/apm-agent-dotnet/pull/544), [#545](https://github.com/elastic/apm-agent-dotnet/pull/545).

## 1.1.1 [elastic-apm-net-agent-111-release-notes]

### Features and enhancements [elastic-apm-net-agent-120-features-enhancements]
* Configure transaction max spans. [#472](https://github.com/elastic/apm-agent-dotnet/pull/472)

### Fixes [elastic-apm-net-agent-111-fixes]
* Fixing missing "Date Modified" field on the files from the `1.1.0` packages causing an error while executing `dotnet pack` or `nuget pack` on a project with Elastic APM Agent packages. [#527](https://github.com/elastic/apm-agent-dotnet/pull/527)

## 1.1.0 [elastic-apm-net-agent-110-release-notes]

### Features and enhancements [elastic-apm-net-agent-110-features-enhancements]
* ASP.NET Support, documentation can be found [here](/reference/setup-asp-dot-net.md)
* Central configuration (Beta)

### Fixes [elastic-apm-net-agent-110-fixes]
* Addressed some performance issues [#359](https://github.com/elastic/apm-agent-dotnet/pull/359)
* Improved error handling in ASP.NET Core [#512](https://github.com/elastic/apm-agent-dotnet/pull/512)
* Fix for mono [#164](https://github.com/elastic/apm-agent-dotnet/pull/164)

## 1.0.1 [elastic-apm-net-agent-101-release-notes]

### Features and enhancements [elastic-apm-net-agent-101-features-enhancements]
* Reading request body in ASP.NET Core. Also introduced two new settings: `CaptureBody` and `CaptureBodyContentTypes`. By default this feature is turned off, this is an opt-in feature and can be turned on with the `CaptureBody` setting. [#402](https://github.com/elastic/apm-agent-dotnet/pull/402)

### Fixes [elastic-apm-net-agent-101-fixes]
* `NullReferenceException` on .NET Framework with outgoing HTTP calls created with `HttpClient` in case the response code is HTTP3xx [#450](https://github.com/elastic/apm-agent-dotnet/pull/450)
* Added missing `net461` target to the [`Elastic.Apm`](https://www.nuget.org/packages/Elastic.Apm/) package
* Handling [`Labels`](/reference/public-api.md#api-transaction-tags) with `null` [#429](https://github.com/elastic/apm-agent-dotnet/pull/429)

## 1.0.0 [elastic-apm-net-agent-100-release-notes]
The 1.0.0 GA release of the Elastic APM .NET Agent. Stabilization of the 1.0.0-beta feature for production usage.

### Features and enhancements [elastic-apm-net-agent-100-features-enhancements]
* Out of the box integration with `ILoggerFactory` and the logging  infrastructure in ASP.NET Core [#249](https://github.com/elastic/apm-agent-dotnet/pull/249)
* Introduced `StackTraceLimit` and `SpanFramesMinDurationInMilliseconds` configs [#374](https://github.com/elastic/apm-agent-dotnet/pull/374)
* The Public Agent API now support `Elastic.Apm.Agent.Tracer.CurrentSpan` [#391](https://github.com/elastic/apm-agent-dotnet/pull/391)

### Fixes [elastic-apm-net-agent-100-fixes]
* Thread safety for some bookkeeping around spans [#394](https://github.com/elastic/apm-agent-dotnet/pull/394)
* Auto instrumentation automatically creates sub-spans in case a span is already active [#391](https://github.com/elastic/apm-agent-dotnet/pull/391)

