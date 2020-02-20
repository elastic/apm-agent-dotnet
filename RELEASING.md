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

## Executing the release

After the new changelog and version have been merged to master, the only thing remaining is to run the below commands:


 ```bash
 ## let's assume the origin is your forked repo and upstream the one where the releases are coming from.
 git checkout master

 ## let's ensure we do use the latest commit, although this requirement could be not neccessary if it's required
 ## to use another git commit rather than the HEAD at that time.
 git reset --hard upstream/master

 ## <major>, <minor>, <bug> and <suffix> should be replaced accordingly. <suffix> is an optional one.
 git tag <major>.<minor>.<bug>(-<suffix>)?

 ## Push commit to the usptream repo.
 git push upstream <major>.<minor>.<bug>(-<suffix>)?
 ```

The above commands will push the GitHub tag and will trigger the corresponding [CI Build pipeline](Jenkinsfile) which will run all the required stages to satisfy the release is in a good shape, then at the very end of the pipeline there will be an input approval waiting for an UI interaction to release to the NuGet repo. This particular input approval step will notify by email, to the owners of this repo, regarding the expected action to be done for doing the release.

This release process is a tagged release event based with an input approval.

## Executing the release script locally

If required then it's possible to run the release script locally, for such, the credentials are needed to push to the NuGet repo.

```bash
.ci/linux/release.sh
.ci/linux/deploy.sh <API_KEY> <SERVER_URL>
```

_NOTE_ The above scripts are just wrapper for the `dotnet` command.
