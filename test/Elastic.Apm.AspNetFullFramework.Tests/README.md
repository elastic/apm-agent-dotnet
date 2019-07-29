# Overview

The purpose of `Elastic.Apm.AspNetFullFramework.Tests` project is to provide automated tests for the following two aspects of Elastic APM Agent in the context of ASP.NET:
1. To test features implemented in `Elastic.Apm.AspNetFullFramework` (such as automatic capturing of transactions, etc.)
2. To test features implemented in `Elastic.Apm` (agent's public API, automatic capturing of spans for HTTP calls, metrics, etc.) in the context of a ASP.NET application running on top of IIS.  

## Component (not unit) tests

We use xUnit.net unit testing framework to drive the tests in `Elastic.Apm.AspNetFullFramework.Tests` just as we do in the other test projects but unlike the other test projects `Elastic.Apm.AspNetFullFramework.Tests` tests are not unit tests.
The tests in this projects are much closer to integration tests than unit tests.
But since the agent is the only real component under test and no other component from the stack are involved we call tests in this project _Component Tests_.
In this case .NET agent being the component under test.

Below is the diagram of components and communication between them:
 
![](https://user-images.githubusercontent.com/7782093/59779351-c0f26d00-92c0-11e9-9413-57c05653ced4.png)

# Prerequisites

Prerequisites to execute tests are:

1. Operating system - Windows
2. IIS (10 or later) and ASP.NET installed and IIS started.
3. Process running the tests has environment variable `ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED` set to `true`. There is no need to set this environment variable globally - for example, IIS worker process, running sample application used by the tests, does not depend on this environment variable.
4. Run tests as a user with local administrator permissions (a member in local `Administrators` group).

Requirement to set environment variable `ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED` set to `true` is to have user opt-in.
Before opting-in please note that these tests perform various operations on this machine's IIS such as adding/removing sample applications, stopping/starting application pools, etc.
**So it's not recommended to run these tests on a production environment.**


### How to set environment variable on Windows 

Regarding the prerequisite for having environment variable `ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED` set to `true` - there are various ways to set an environment variable for a process on Windows.

#### Setting environment variable permanently for the current user 

The simplest approach is to set environment variable permanently for the current user.
Permanently in the sense that the environment variable will stay set after logout/reboot.
The steps are:
1. Open the Environment Variables dialog box
    * Go to Start Menu's Search
    * Search for and select `Edit environment variables for your account`
2. Create a new environment variable:
    * In the User variables section, click New.
    * The New User Variable dialog box opens.
    * Enter the name of the variable and its value, and click OK.
    * The New User Variable dialog box closes, and the variable is added to the User variables section of the Environment Variables dialog box.
    * Click OK in the Environment Variables dialog box.

#### Setting environment variable just for the current session

Another approach is to set environment variable just for the current session of of an IDE (Visual Studio or Jetbrains Rider) or command line execution of unit tests. 
The steps are:
1. Start a console (for example `cmd.exe`). **Make sure to use `Run as administrator`**
2. Set the environment variable by executing `set ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED=true`
3. Start an IDE (for example `"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.exe"`) or run tests directly at the console via command `dotnet.exe test test\Elastic.Apm.AspNetFullFramework.Tests -v n -r target -d target\diag.log --no-build` (assumes that the current directory is the root of the solution - the directory with `ElasticApmAgent.sln`)    

# Troubleshooting

### Tests fail to change IIS configuration because of User Account Control (UAC)

#### Symptom
Tests fail with an exception

```
Message: System.UnauthorizedAccessException : Filename: redirection.config
Error: Cannot read configuration file due to insufficient permissions
```

thrown from 

```
using (var serverManager = new ServerManager())
```
in `test/Elastic.Apm.AspNetFullFramework.Tests/IisAdministration.cs` 

#### Resolution

Make sure the process that runs the tests was started using `Run as administrator`.
For example if you used `cmd.exe` console make sure that title of `cmd.exe` window starts with `Administrator: `    


### Tests fail with result message `Expected response.StatusCode to be XYZ, but found ServiceUnavailable`

#### Symptom

Tests fail with result message 

```
Expected response.StatusCode to be 404, but found ServiceUnavailable.
```

In your case it might be a different HTTP status, for example 200 and not 404.  

#### Resolution

Make sure you do not have IIS Manager console or [Process Monitor (AKA procmon)](https://docs.microsoft.com/en-us/sysinternals/downloads/procmon) open.    

You can find more detailed explanation [here](https://github.com/elastic/apm-agent-dotnet/pull/273#issuecomment-503685801).

# Tips and Tricks

## Preventing IIS items (app, pool, etc.) from being torn down after each test

By default after each test all the items added to IIS (that is sample application and its application pool) are automatically removed as part of testing _Tear-Down_ stage.
If you would like to inspect those items you can prevent them from being torn down by setting `ELASTIC_APM_TESTS_FULL_FRAMEWORK_TEAR_DOWN_PERSISTENT_DATA` environment variable to `false`.
**Please note that in this case it's up to you to remove those items from IIS.**
The tests remove/add sample application and its application pool at the start of each test so it does not matter to the tests if the previous test run cleaned up after itself or if it did not.    
