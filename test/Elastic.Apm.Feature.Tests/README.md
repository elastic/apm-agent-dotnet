# Elastic.Apm.Feature.Tests

This project contains [gherkin](https://cucumber.io/docs/gherkin/) specifications and their implementation in C# which test the agent.

The specification from the `/Features` folder is synced from the [`github.com/elastic/apm`](https://github.com/elastic/apm) repository from the [`tests/agents/gherkin-specs`](https://github.com/elastic/apm/tree/main/tests/agents/gherkin-specs) folder via automated PRs.
Every APM Agent implements these spec files and therefore they are managed centrally in the `/elastic/apm` repository.

Spec files (files with `.feature` extension) in this project should not be changed - in case we need to adapt the specification, a PR in the [`github.com/elastic/apm`](https://github.com/elastic/apm) needs to be opened.
That way changes in the spec files are synced across all APM Agents.