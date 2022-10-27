# Elastic.Apm.Tests.Utilities

This project contains reusable utilities (e.g. in-memory or mock implementation of agent components,
test inputs, etc.) which are used in other test projects.

## json-specs

The folder `/TestResources/json-specs` contains test specifications in JSON format that are used as test inputs in
different projects (e.g. `Elastic.Apm.Tests`).

These specifications are synchronized from the [`github.com/elastic/apm`](https://github.com/elastic/apm) repository from the
[`tests/agents/json-specs`](https://github.com/elastic/apm/tree/main/tests/agents/json-specs) folder
via automated PRs. Every APM Agent implements these JSON spec files and therefore those are managed centrally
in the `/elastic/apm` repository.

JSON spec files in *this* project should not be changed - in case we need to adapt the specification,
a PR in [`github.com/elastic/apm`](https://github.com/elastic/apm) needs to be opened.
That way, changes in the spec files are synced across all APM Agents.
