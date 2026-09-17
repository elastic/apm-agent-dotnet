---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/upgrading.html
description: "Guidance for upgrading the Elastic APM .NET Agent between versions, including links to release notes and agent-server compatibility information."
navigation_title: Upgrading
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Upgrading [upgrading]

Upgrades between minor versions of the agent, like from 1.1 to 1.2 are always backwards compatible. Upgrades that involve a major version bump often come with some backwards incompatible changes.

Before upgrading the agent, be sure to review the:

* [Breaking changes](/release-notes/breaking-changes.md) for every version between your current version and the target version
* [Agent release notes](/release-notes/index.md)
* [Agent and Server compatibility chart](docs-content://solutions/observability/apm/apm-agent-compatibility.md)

We always recommend testing upgrades in a non-production environment before applying them to production.

## Upgrading profiler auto instrumentation [upgrading-profiler]

The following steps apply to Windows hosts running IIS. For other platforms and deployment environments, the same principles apply: stop the application, replace the profiler files, and restart.

There are two approaches. The versioned directory approach is recommended because it avoids a downtime window and makes rollback straightforward.

::::{note}
Environment variable names differ by runtime. Use the `COR_` prefix for .NET Framework App Pools and the `CORECLR_` prefix for .NET (formerly .NET Core) App Pools. If you have both types on the same host, configure each App Pool with the appropriate prefix.
::::

### Option 1: Versioned directory (recommended) [upgrading-profiler-versioned]

Extract the new profiler alongside the old one and update the environment variables to point to it. IIS does not need to be fully stopped, and rollback is a matter of reverting the environment variables.

1. Download the new profiler zip from the [GitHub Releases page](https://github.com/elastic/apm-agent-dotnet/releases).
2. Extract the new profiler zip into a new versioned directory, for example `C:\elastic\apm-agent-dotnet\1.35.0`.
3. Update the following environment variables at the appropriate location (per Application Pool via AppCmd, or machine-wide) to point to the new directory:
   * `COR_PROFILER_PATH` (for .NET Framework App Pools) or `CORECLR_PROFILER_PATH` (for .NET App Pools)
   * `ELASTIC_APM_PROFILER_HOME`
   * `ELASTIC_APM_PROFILER_INTEGRATIONS` - only if this was explicitly set; otherwise the profiler locates `integrations.yml` automatically within `ELASTIC_APM_PROFILER_HOME`
4. Update `web.config` binding redirects if required. Refer to [Binding redirects](#upgrading-profiler-binding-redirects) below.
5. Restart the individual instrumented Application Pools or the whole IIS service:
   * If environment variables are set per Application Pool, App Pools can be recycled individually and no full IIS stop is required.
   * If environment variables are set machine-wide, a full IIS restart is required:

     ```powershell
     Stop-Service WAS -Force
     Start-Service W3SVC
     ```

6. Verify the application launches and check the profiler log files (`%PROGRAMDATA%\elastic\apm-agent-dotnet\logs` by default) and trace data in Elastic Observability.

To roll back, update the environment variables to point to the previous directory and restart the affected App Pools or IIS.

### Option 2: In-place replacement [upgrading-profiler-inplace]

Replace the profiler files in the existing directory. IIS must be fully stopped before the old files are removed.

1. Download the new profiler zip from the [GitHub Releases page](https://github.com/elastic/apm-agent-dotnet/releases).
2. Stop IIS to release the profiler files held open by instrumented worker processes:

   ```powershell
   Stop-Service WAS -Force
   ```

3. Delete the contents of the existing profiler directory (files and subdirectories). Do not overwrite in place as some releases remove files from the package, and stale files left behind by an in-place overwrite can cause unexpected behavior.
4. Extract the new profiler zip into the same directory.
5. Update `web.config` binding redirects if required. Refer to [Binding redirects](#upgrading-profiler-binding-redirects) below.
6. Start IIS:

   ```powershell
   Start-Service W3SVC
   ```

7. Verify the application launches and check the profiler log files (`%PROGRAMDATA%\elastic\apm-agent-dotnet\logs` by default) and trace data in Elastic Observability.

### Binding redirects [upgrading-profiler-binding-redirects]

This section applies to classic ASP.NET applications running on .NET Framework only.

When the `Elastic.Apm` package bumps the version of a .NET Framework dependency, hand-edited binding redirects for those assemblies in `web.config` may need updating. Check the [breaking changes](/release-notes/breaking-changes.md) for the versions you are upgrading across to identify which assemblies were affected.

For each affected assembly, check your application's `bin` directory for the corresponding DLL. If the DLL is not present, no action is needed. If it is present and you have a hand-edited binding redirect for that assembly in `web.config`, update the `oldVersion` upper bound and `newVersion` to the new version, or delete the entry and let MSBuild regenerate it. If regenerating, rebuild the application and redeploy the updated `web.config` before restarting IIS.

If your binding redirects are MSBuild-generated (the default for most projects), they are updated automatically at the next build and no manual action is required.

### Other considerations [upgrading-profiler-other-considerations]

**NuGet packages alongside the profiler.** If you use any `Elastic.Apm.*` NuGet packages alongside the profiler (for example, `Elastic.Apm` for custom spans, or `Elastic.Apm.EntityFramework6` for EF6 instrumentation) update every `Elastic.Apm.*` package to the same version as the profiler and rebuild and redeploy the application before restarting IIS. A version mismatch between the profiler and any NuGet package will cause errors at startup.

**Machine-wide environment variables (Option 2 only).** If profiler environment variables are configured at the machine level rather than per Application Pool, any .NET process that starts between step 3 and step 4 starts without profiler instrumentation. This is not a failure and the process starts normally, but that instance is not instrumented until it is restarted after the new profiler files are in place.

## End of life dates [end-of-life-dates]

We love all our products, but sometimes we must say goodbye to a release so that we can continue moving forward on future development and innovation. Our [End of life policy](https://www.elastic.co/support/eol) defines how long a given release is considered supported, as well as how long a release is considered still in active development or maintenance.

