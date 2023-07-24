# Running Azure Tests Locally

*These steps have been tested on Windows.*

## Dependencies

In order to run Azure tests, a few dependencies must be installed locally:

1. Install the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).
2. Download [Terraform.exe](https://developer.hashicorp.com/terraform/downloads) to 
a local directory and add the path to the system environment variables.

For the Azure Functions tests

3. Install [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) 
which is required to execute the Azure Functions tests.
4. Install [Azure Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-emulator#get-the-storage-emulator) either via the SDK or standalone.

## Set Environment Variables

*Optional*: Create a environment variable named "AZURE_RESOURCE_GROUP_PREFIX" with 
a custom prefix "dotnet-apm-local-dev-<YOURNAME>". e.g. "dotnet-apm-local-dev-stevegordon". 
This can be used to identify the resource group created in Azure when deploying 
the required resources.

## Azure Login

You will require an Azure account and a user with appropriate permissions to deploy
resources. For Elastic engineers, we can use our Azure account.

You will require an account to login to the [Azure Cloud Platform](https://github.com/elastic/cloud/blob/ac5f979188cd6f9293b7461ee7c05004a614d4fe/wiki/Azure.md).

You will requires access to the `client-dev` subscription.

Perform a login using the Azure CLI, which will redirect you to a browser to 
authenticate with Azure using SSO:

`az login`

The result will list the subscriptions you can access. Ensure that `client-dev` 
is the default. 

Set your default subscription using:

`az account set --subscription {YourSubscriptionId}`

Verify the current subscription:

`az account show`

## Run Tests

You can execute the Azure tests using `dotnet test` from a terminal (you may require 
administrator priviledges). To run only the Azure tests you can specifically 
filter them:

`dotnet test -c Release --filter:"FullyQualifiedName~Elastic.Apm.Azure" -f net7.0`

This will run all tests for Azure services. It will run them again the .NET 7 
target framework. On Windows, without specifying the framework, it will run against 
all targets (if the tests multitarget). You can further limit to a specific 
service by updating the FullyQualifiedName in the filter.

You can also run tests from an IDE such as Visual Studio Test Explorer.