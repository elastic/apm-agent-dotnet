## CI/CD

There are 4 main stages that run on GitHub actions:

* Linting
* Pack
* Test
* Release

### Scenarios

* Tests should be triggered on branch, tag and PR basis.
* Commits that are only affecting the docs files should not trigger any test or similar stages that are not required.
* Pull Requests that are only affecting the docs files should not trigger any test or similar stages that are not required.
* Automated release in the CI gets triggered when a tag release is created.
* **This is not the case yet**, but if Github secrets are required then Pull Requests from forked repositories won't run any build accessing those secrets. If needed, then create a feature branch.

### How to interact with the CI?

#### On a PR basis

Once a PR has been opened then there are two different ways you can trigger builds in the CI:

1. Commit based
1. UI based, any Elasticians can force a build through the GitHub UI

#### Branches

Every time there is a merge to main or any release branches the whole workflow will compile and test every entry in the compatibility matrix for Linux and Windows.

### Release process

This process has been fully automated and it gets triggered when a tag release has been created.
The tag release follows the naming convention: `v.<major>.<minor>.<patch>`, where `<major>`, `<minor>` and `<patch>`.

### OpenTelemetry

Every workflow and its logs are exported to OpenTelemetry traces/logs/metrics. Those details can be seen [here](https://ela.st/oblt-ci-cd-stats) (**NOTE**: only available for Elasticians).

## Bump automation

[updatecli](https://www.updatecli.io/) is the tool we use to automatically update the specs 
the [APM agents](./updatecli.yml) use.
