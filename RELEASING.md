# Releasing a new version

This guide aims to provide guidance on how to release new versions of the `apm-agent-dotnet` binary as well as updating all the necessary parts to make it successful.

## Prerequisites

Releasing a new version of the binary implies that there have been changes in the source code which are meant to be released for wider consumption. Before releasing a new version there's some prerequisites that have to be checked.

### Make sure the version is updated

Since the source has changed, we need to update the current committed version to a higher version so that the release is published.

The version is currently defined in the [Directory.Build.props](./src/Directory.Build.props) in the [SEMVER](https://semver.org) format: `MAJOR.MINOR.BUG`

Say we want to perform a minor version release (i.e. no breaking changes and only new features and bug fixes are being included); in which case we'll update the _MINOR_ part of the version.

For instance, the bump from version 1.1.2 to 1.2 can be found in this https://github.com/elastic/apm-agent-dotnet/pull/624

### Generating a changelog for the new version

Once the version is updated, we can then generate the changelog, you can see the current changelog in [`CHANGELOG.asciidoc`](CHANGELOG.asciidoc). The idea is to fill all the applicable sections so that users can consume an orderly changelog.

For major and minor releases, you'll also need to update the EOL table in [`upgrading.asciidoc`](docs/upgrading.asciidoc). The EOL date is the release date plus 18 months.

After a changelog has been manually curated, a new pull request can be opened with the changelog and version update changes (and EOL table if applicable).

For instance, the changelog that was created for the 1.2 release can be found in this https://github.com/elastic/apm-agent-dotnet/pull/640

## Releasing a new package

In case you release a package the first time and you rely on the CI to push that to nuget.org, you need to make sure that the [deploy.sh](https://github.com/elastic/apm-agent-dotnet/blob/main/.ci/linux/deploy.sh) script is updated. You need to add the name of the new package into that script.

## Executing the release

After the new changelog and version have been merged to main, the only thing remaining is to run the below commands:


 ```bash
 ## let's assume the origin is your forked repo and upstream the one where the releases are coming from.
 git checkout main

 ## let's ensure we do use the latest commit, although this requirement could be not necessary if it's required
 ## to use another git commit rather than the HEAD at that time.
 git reset --hard upstream/main

 ## <major>, <minor>, <bug> and <suffix> should be replaced accordingly. <suffix> is an optional one.
 git tag v<major>.<minor>.<bug>(-<suffix>)?

 ## Push commit to the usptream repo.
 git push upstream v<major>.<minor>.<bug>(-<suffix>)?
 ```

The above commands will push the GitHub tag and will trigger the corresponding [CI Build pipeline](Jenkinsfile) which will run all the required stages to satisfy the release is in a good shape, then at the very end of the pipeline there will be an input approval waiting for an UI interaction to release to the NuGet repo. This particular input approval step will notify by email, to the owners of this repo, regarding the expected action to be done for doing the release.

Tag names should start with a `v` prefix.

This release process is a tagged release event based with an input approval.

## Steps after the release 

### Attaching files on GitHub

We attach 2 files to the release on GitHub:
- `ElasticApmAgent_<major>.<minor>.<bug>(-<suffix>)?.zip`: This is the startup-hook based agent.
- `elastic_apm_profiler_<major>.<minor>.<bug>(-<suffix>)?.zip`: This is the profiler-based agent. The CI currently generates 2 profiler zip files, one for Windows (with `libelastic_apm_profiler.dll`), and one for Linux (with `libelastic_apm_profiler.so`). The only difference in the zip files is the native agent, the remaining files are the same. We copy the 2 native files into a folder with the remaining files and zip that folder as `elastic_apm_profiler_<major>.<minor>.<bug>(-<suffix>)?.zip` which we attach to the release.

The steps above aren't currently automated. All the necessary files can be found under "Artifacts" in Jenkins and the files need to be manually attached to the release on GitHub.

### Updating the documentation

Each major version has a `<major>.x` branch in the repository (e.g. for major version `1` we have the branch `1.x`).

In case of minor and patch releases we just need to update the `<major>.x` branch to the currently released tag:

 ```bash
git checkout v<major>.<minor>.<bug>(-<suffix>)? -b <major>.x

git push --force  upstream
 ```

In case of a major release, we need to create the `<major>.x` branch from the currently released tag and push the new `<major>.x` branch.

Additionally, in case of a major version release, we need to create a PR in [elastic/docs](https://github.com/elastic/docs).

In this PR we need to update:
- [`conf.yaml`](https://github.com/elastic/docs/blob/master/conf.yaml): Set the `current` part to the new `<major>.x` and add that to the `branches` and `live` parts. In addition, remove the previous major entry from the `live` key.
- [`shared/versions/stack/*.asciidoc`](https://github.com/elastic/docs/tree/master/shared/versions/stack): This directory defines how links from stack-versioned documentation relate to links from non stack-versioned documentation. For example, in the `8.5` file, the variable `:apm-dotnet-branch:` is set to `1.x`. This means any links in the `8.5` stack docs (like the APM Guide) that point to the APM .NET Agent reference, will point to the `1.x` version of those docs. The number of files you update in this directory depends on version compatibility between stack docs and your APM agent. In general, we update as far back as the new version of the agent is compatible with the stack; this pushes new documentation to the user.

### Prepare the next (pre-release) version

To clearly distinguish pre-release builds and artifacts from the newly released ones,
bump the agent version again by adding the `-alpha` suffix to the following files:

- `/src/Directory.Build.props`:

  ```xml
  ...
  <InformationalVersion>1.20.0-alpha</InformationalVersion>
  <VersionPrefix>1.20.0-alpha</VersionPrefix>
  ...
  ```

  **Note** that `AssemblyVersion` and `FileVersion` do not add the `-alpha` prefix.

- `/src/elastic_apm_profiler/Cargo.toml`:

  ```toml
  ...
  version = "1.20.0-alpha"
  ...
  ```

## Executing the release script locally

If required then it's possible to run the release script locally, for such, the credentials are needed to push to the NuGet repo.

```bash
.ci/linux/release.sh
.ci/linux/deploy.sh <API_KEY> <SERVER_URL>
```

_NOTE_ The above scripts are just wrapper for the `dotnet` command.
