# Contributing to the Elastic APM .NET agent

The .NET APM Agent is open source and we love to receive contributions from our community — you!

There are many ways to contribute,
from writing tutorials or blog posts,
improving the documentation,
submitting bug reports and feature requests or writing code.

You can get in touch with us through [Discuss](https://discuss.elastic.co/c/apm),
feedback and ideas are always welcome.

## Prerequisites

In order to build the source code of the .NET APM Agent you need
 * .NET Core 2.1 or later

You can use any IDE that supports .NET Core development and you can use any OS that is supported by .NET Core.

## Code contributions (please read this before your first PR)

If you have a bugfix or new feature that you would like to contribute,
please find or open an issue about it first.
Talk about what you would like to do.
It may be that somebody is already working on it,
or that there are particular issues that you should know about before implementing the change.

We aim to maintain high code quality, therefore every PR goes through the same review process, no matter who opened the PR.

### Submitting your changes

Generally, we require that you test any code you add or modify. This means you run all existing tests and in case you add new code you make sure it's covered by at least 1 test.

**No new feature will be merged without tests covering it.**

Once your changes are ready to submit for review:

1. Sign the Contributor License Agreement

    Please make sure you have signed our [Contributor License Agreement](https://www.elastic.co/contributor-agreement/). The CLA checker identifies your commits based on your email address, so make sure you sign the CLA with the same mail address that you use for your commits. A single commit in a PR with an email address with no CLA signature will prevent us from merging your PR.

    We are not asking you to assign copyright to us,
    but to give us the right to distribute your code without restriction.
    We ask this of all contributors in order to assure our users of the origin and continuing existence of the code.
    You only need to sign the CLA once.

2. Test your changes

    Run the test suite to make sure that nothing is broken.
    See [testing](#testing) for details.

3. Rebase your changes

    Update your local repository with the most recent code from the main repo,
    and rebase your branch on top of the latest master branch.
    We prefer your initial changes to be squashed into a single commit.
    Later,
    if we ask you to make changes,
    add them as separate commits.
    This makes them easier to review.
    As a final step before merging, we will either ask you to squash all commits yourself or we'll do it for you.

4. Submit a pull request

    Push your local changes to your forked copy of the repository and [submit a pull request](https://help.github.com/articles/using-pull-requests).
    In the pull request,
    choose a title which sums up the changes that you have made,
    and in the body provide more details about what your changes do.
    Also mention the number of the issue where the discussion has taken place,
    eg "Closes #123".

5. Be patient

    We might not be able to review your code as fast as we would like to,
    but we'll do our best to dedicate it the attention it deserves.
    Your effort is much appreciated!

### Testing

The test suite consists of xUnit tests.

To run all tests, including the integration tests, execute `dotnet test` on every project from the `/test` folder.

### Workflow

All feature development and most bug fixes hit the master branch first.
Pull requests should be reviewed by someone with commit access.
Once approved, the author of the pull request,
or reviewer if the author does not have commit access,
should "Squash and merge".

### General advice

Please make sure that your PR addresses a single issue and its size is reasonable to review. There are no hard rules, but please keep in mind that every line of your PR will be reviewed and it's not uncommon that a longer discussion evolves in case of a sensitive change.

Therefore it's preferred to have multiple smaller PRs over a single big PR.

This makes sure that changes that have consensus get merged quickly and don't get blocked by unrelated changes. Additionally, the repository will also have a useful and searchable history by doing so.

Please do:
- Try to get feedback as early as possible and prefer to ask questions in issues before you start submitting code.
- Add a description to your PR which describes your intention and gives a high level summary about your changes.
- Run all the tests from the `test` folder and make sure that all of them are green. See [Testing](###Testing).
- In case of new code, make sure it's covered by at least 1 test.
- make sure your IDE uses the `.editorconfig` from the repo and you follow our coding guidelines. See [Coding-guidelines](###Coding-guidelines).
- Feel free to close a PR and create a new one in case you changed your mind, or found a better solution.
- Feel free to fix typos.
- Feel free to use the draft PR feature of GitHub, in case you would like to show some work in progress code and get feedback on it.

Please don't:
- Create a PR where you address multiple issues at once.
- Create a giant PR with a huge change set. There is no hard rule, but if your change grows over 1000 lines, it's maybe worth thinking about making it self contained and submit it as a PR and address follow-up issues in a subsequent PR. (of course there can be exceptions)
- Actively push code to a PR until you have received feedback. Of course if you spot some minor things after you opened a PR, it's perfectly fine to push a small fix. But please don't do active work on a PR that haven't received any feedback. It's very possible that someone already looked at it and is about to write a detailed review. If you actively add new changes to a PR then the reviewer will have a hard time to provide up to date feedback. If you just want to show work-in-progress code, feel free to use the draft feature of github, or indicate in the title, that the work is not 100% done yet. Of course, once you have feedback on a PR, it's perfectly fine, or rather encouraged to start working collaboratively on the PR and push new changes and address issues and suggestions from the reviewer.
- Change or add dependencies, unless you are really sure about it (it's best to ask about this in an issue first) - see [compatibility](####compatibility).

### Design considerations

#### Performance

The agent is designed to monitor production applications. Therefore it's very important to keep the performance overhead of the agent as low as possible.

It's not uncommon that you write or change code that can potentially change the performance characteristics of the agent and therefore also of the application's of our users.

If this is the case then a perf. test should be added to the `test\Elastic.Apm.Benchmarks` project which proves that the new code does not make the performance of the agent worse than it was before your PR.

We care both about memory and CPU overhead and both should be measured. The `test\Elastic.Apm.Benchmarks` is configured to measure both.

#### Do not crash the monitored process

The agent must never crash the monitored application.

One aspect of this is that the agent code itself must never throws exceptions. Instead, the agent uses the logger to log errors and it either skips specific events or completely stops collecting events. Skipping events must be preferred over unhandled exceptions in agent code.

The other aspect of this is that each time the agent calls other libraries, it makes sure no call causes an exception or an case an exception is thrown the agent handles it and makes sure these exceptions never bubble up to the monitored application.

#### Compatibility

We aim to support the broadest set of .NET flavors and versions possible. This includes different OSs, typically Windows, Linux, and macOS.

This is especially true for versions officially supported by Microsoft at the moment. In case of specific libraries and frameworks (e.g. ASP.NET Core) we aim to support every version which is supported by Microsoft or the library/framework author.

This means that the .NET APM Agent can end up in very different environments.

To allow the agent to be used in all these different environments there are 2 categories of projects in the `src` folder:

 - The `Elastic.Apm` project is the core of the agent. This package is referenced by every other agent package, and it contains the Public Agent API. Therefore this package must target a very broad set of .NET flavors, versions, and environments. Currently this package targets .NET Standard 2.0. This means .NET Framework 3.5 is not supported currently, but the code-base would be very easy to port to .NET Framework 3.5. We should aim for a code-base in this project that is very easy to port to different environments. Porting it to older environments can be triggered by user feedback - e.g. someone needs the agent in an older environment.

 - Framework and Library specific projects (e.g. `Elastic.Apm.AspNetCore`). In case of these packages the goal is to target every possible supported (by Microsoft or the Library/Framework author) version of the corresponding library.

Therefore, in case of:
- `Elastic.Apm`: If you want to add a dependency, make sure that this dependency does not make the agent unusable in some specific environments. Furthermore, make sure that the new dependency does not make the agent impossible to port to a different environment (e.g. making it a shared library, or creating a classic .NET Framework library from that package should still be feasible after you introduce a dependency). One typical example is "logging libraries": Logging is spread across the whole project, and depending on a specific logging library would mean that the target frameworks and environments of the `Elastic.Apm` package are dictated by the dependency. If the target framework of the dependency is "everything in the current and every future release" (very rare), then it's ok to take on this dependency. Otherwise there should be a thoughtful discussion in an issue and a broad agreement on introducing this new dependency.
In case of existing dependencies we need to depend on the oldest possible version. With this the application that uses the agent library can also depend on the same or any newer version. This would not be true the other way around.

- Framework and Library specific projects: It's ok to have dependencies that are typical dependencies of applications depending on the specific framework/library package. E.g. in case of  `Elastic.Apm.AspNetCore` it's safe to assume that a "typical" ASP.NET Core application has everything that is part of the [Microsoft.AspNetCore.All](https://www.nuget.org/packages/Microsoft.AspNetCore.All) package as its dependency. Similarly to `Elastic.Apm` every dependency has to have the lowest possible version. E.g. depending on `"Microsoft.AspNetCore.Http.Abstractions" Version="2.0.0" ` makes the  `Elastic.Apm.AspNetCore` project usable with every ASP.NET Core version that is at least 2.0.0 or newer. Targeting e.g. version 2.2.0 would mean that versions older than 2.2.0 are not supported.


### Coding guidelines

The repository contains a [.editorconfig file](.editorconfig) which is automatically picked up by Visual Studio and JetBrains Rider. Additionally, we also have a [.DotSettings file](ElasticApmAgent.sln.DotSettings). If you auto-format your code in Visual Studio or Rider, you will automatically confirm to our code styling. If you want to know more details about the specific conventions we use, feel free to look into the [.editorconfig file](.editorconfig).

### Third party dependencies

In case an agent project depends on a NuGet package, the dependency does not have to be listed, since it's not directly shipped with the agent. 

However if source code from a third party dependency is added to the agent, the following steps must be taken:
1. Make sure the license of the third party dependency is one that is permitted to be included in the agent, based on Elastic's Open Source Policy
1. List the dependency in the local `NOTICE` file of the project. Also include the full text of the dependency's license. One exception here is the Apache v2 license text, which does not have to be copied, since the agent itself already has that text as its own license.
1. List the dependency in the local `LICENSE_LOCAL` file of the project including the the full text of the dependency's license (except Apache v2). This file will be [merged](https://github.com/elastic/apm-agent-dotnet/blob/master/src/Directory.Build.targets#L2) with the root [`LICENSE`](https://github.com/elastic/apm-agent-dotnet/blob/master/LICENSE) file.
1. Make sure in the project's `.csproj` file that the `NOTICE` and the generated `LICENSE` file is packed with the project.

An example of this can be seen in the [`Elastic.Apm.MongoDb`](https://github.com/elastic/apm-agent-dotnet/tree/master/src/Elastic.Apm.MongoDb) project.

### Adding support for instrumenting new libraries/frameworks/APIs

Coming soon

### Releasing

See [RELEASING.md](RELEASING.md).
