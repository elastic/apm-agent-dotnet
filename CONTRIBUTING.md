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

## Code contributions

If you have a bugfix or new feature that you would like to contribute,
please find or open an issue about it first.
Talk about what you would like to do.
It may be that somebody is already working on it,
or that there are particular issues that you should know about before implementing the change.

### Submitting your changes

Generally, we require that you test any code you are adding or modifying.
Once your changes are ready to submit for review:

1. Sign the Contributor License Agreement

    Please make sure you have signed our [Contributor License Agreement](https://www.elastic.co/contributor-agreement/).
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
    As a final step before merging we will either ask you to squash all commits yourself or we'll do it for you.

4. Submit a pull request

    Push your local changes to your forked copy of the repository and [submit a pull request](https://help.github.com/articles/using-pull-requests).
    In the pull request,
    choose a title which sums up the changes that you have made,
    and in the body provide more details about what your changes do.
    Also mention the number of the issue where discussion has taken place,
    eg "Closes #123".

5. Be patient

    We might not be able to review your code as fast as we would like to,
    but we'll do our best to dedicate it the attention it deserves.
    Your effort is much appreciated!

### Testing

The test suite consists of xUnit tests.

To run all tests, including the integration tests, execute these two commands from the root of the repository:

```bash
dotnet test test/Elastic.Apm.Tests/
```

```bash
dotnet test test/Elastic.Apm.AspNetCore.Tests/
```

### Workflow

All feature development and most bug fixes hit the master branch first.
Pull requests should be reviewed by someone with commit access.
Once approved, the author of the pull request,
or reviewer if the author does not have commit access,
should "Squash and merge".

### Coding guidelines

Coming soon.

### Adding support for instrumenting new libraries/frameworks/APIs

Coming soon

### Releasing

Coming soon.