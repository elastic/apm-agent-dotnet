# Releasing a new version

This guide aims to provide guidance on how to release new versions of the `apm-agent-dotnet` binary as well as updating all the necessary parts to make it successful.

## Prerequisites

Releasing a new version of the binary implies that there have been changes in the source code which are meant to be released for wider consumption. Before releasing a new version there's some prerequisites that have to be checked.

### Versioning

Versioning off the binaries is automated through [minver](https://github.com/adamralph/minver)

The tag dictates the version number. Untagged commits are automatically versioned as prereleases according to their distance to their closest version tag.

### Generating a changelog for the new version

Prior to tagging and releasing the new version we create a changelog commit. Adding relevant new features and bugfixes to [`CHANGELOG.asciidoc`](CHANGELOG.asciidoc). The idea is to fill all the applicable sections so that users can consume an orderly changelog.

After a changelog has been manually curated, a new pull request must be opened with the changelog.

For instance, the changelog that was created for the 1.2 release can be found in this https://github.com/elastic/apm-agent-dotnet/pull/640

## Releasing a new package

In case you release a package the first time and you rely on the CI to push that to nuget.org, you need to make sure that t [deploy.sh](https://github.com/elastic/apm-agent-dotnet/blob/main/.ci/linux/deploy.sh) script is updated. You need to add the name of the new package into that script.

## Executing the release

After the new changelog has been merged to main.

Create a new release on Github, creating a new tag for the version `vMAJOR.MINOR.PATCH`

Creating a release will trigger the [release Github Action](.github/workflows/release.yml)

## Steps after the release 

### Attaching files on GitHub

> **NOTE**: The steps below are now automated with GitHub actions.

We attach 2 files to the release on GitHub:
- `ElasticApmAgent_<major>.<minor>.<bug>(-<suffix>)?.zip`: This is the startup-hook based agent.
- `elastic_apm_profiler_<major>.<minor>.<bug>(-<suffix>)?.zip`: This is the profiler-based agent. The CI currently generates 2 profiler zip files, one for Windows (with `libelastic_apm_profiler.dll`), and one for Linux (with `libelastic_apm_profiler.so`). The only difference in the zip files is the native agent, the remaining files are the same. We copy the 2 native files into a folder with the remaining files and zip that folder as `elastic_apm_profiler_<major>.<minor>.<bug>(-<suffix>)?.zip` which we attach to the release.


## Executing the release script locally

If required then it's possible to run the release script locally, for such, the credentials are needed to push to the NuGet repo.

```bash
./build.sh pack
.ci/linux/deploy.sh <API_KEY> <SERVER_URL>
```

_NOTE_ The above scripts are just wrapper for the `dotnet` command.
