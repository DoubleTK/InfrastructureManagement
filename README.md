# Infrastructure Management

Welcome to the Infrastructure Management take-home assessment repository for UIPath.

## About The Project

This project is a take-home assessment dealing with infrastructure management. The goal of this project is to provide
a simple API that allows for provisioning and maintaining various cloud resources.

## Prerequisites

- Docker (or Podman) for running the AppHost and integration tests.
  - .NET Aspire uses containers for managing the various resources it is responsible for.
  - OddDotNet runs in a container as well.
- The latest .NET 9.0 SDK, found [here](https://dotnet.microsoft.com/en-us/download).
- Free (or higher) tier Azure account, with a token generated.
- The Azure CLI, for generating credentials to access Azure resources. Installation instructions for the various OS's
located [here](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).
- 

## Requirements

Per the take-home assessment documentation, the following functionality will be provided:

### Provisioning of Cloud Resources


## Environment Configuration

This project requires a Service Principal to authenticate and authorize with Azure resources. I generally lean towards
a Managed Identity instead, but Service Principal has been around a bit longer and is a bit simpler to set up.

The Service Principal is created automatically when you register a new App. To do so, run the following command:

```shell
az ad sp create-for-rbac --name InfrastructureManagement
```

> **_NOTE:_** The Azure CLI must be installed and you must have logged in to your subscription before running the above
> command.

The output of this command includes the required credentials for managing Azure resources in code. Copy this output to
your User Secrets file.

```json
{
  "appId": "",
  "displayName": "",
  "password": "",
  "tenant": ""
}

```

> **_NOTE:_** User Secrets are the .NET way for managing secret credentials while developing locally. The User Secrets
> file for this project can be found at `~/.microsoft/usersecrets/56a7c94b-147e-4d04-aaba-0e93358fa28a/secrets.json` on
> MacOS and Linux, or `%APPDATA%\Microsoft\UserSecrets\56a7c94b-147e-4d04-aaba-0e93358fa28a\secrets.json` on Windows.

The keys of the values you pasted above need to be modified to match expected environment variables that Azure Identity
`DefaultAzureCredential` uses. The required variables are:
- AZURE_TENANT_ID - update "tenant" to this value
- AZURE_CLIENT_ID - update "appId" to this value
- AZURE_CLIENT_SECRET - update "password" to this value

Your user secrets should look like this:

```json
{
  "AZURE_TENANT_ID": "...",
  "AZURE_CLIENT_ID": "...",
  "AZURE_CLIENT_SECRET": "..."
}
```

Display name is not needed and can be removed. These environment variables will be automatically pulled in during 
application startup and used by the `DefaultAzureCredential` provider to enable access to Azure resources.

Once you have created the App Registration and Service Principal, and saved the output, we need to add this Service
Principal to a Microsoft Entra group. First, we'll create the AD group:

```shell
az ad group create \
  --display-name UIPath \
  --mail-nickname UIPath
```
After creating the group, we'll add the Service Principal to the group, but first we need to know its object ID:

```shell
az ad sp list \
  --filter "startswith(displayName, 'InfrastructureManagement')" \
  --query "[].{objectId:id, displayName:displayName}"
```
Using the object ID obtained from this command, we'll now register it as part of the UIPath AD group (be sure to replace
"<object-id>" with the ID from the above query):

```shell
az ad group member add \
  --group UIPath \
  --member-id <object-id>
```
Assuming no errors showed up in the console, the InfrastructureManagement Service Principal has now been added as a 
member of the UIPath group. We can now assign the appropriate roles for managing Azure resources.

For simplicity, we'll be assigning the "Contributor" role to the group. A more granular role(s) may be desired in a production
environment, but for simplicity we'll stick with Contributor. You'll need the subscription ID to make this happen:

```shell
az account list
```

After obtaining the subscription ID, assign the role to the group (replace <object-id> with the InfrastructureManagement 
ID, and <subscription-id> with your subscription ID):

```shell
az role assignment create --assignee "<object-id>" \
  --role "Contributor" \
  --scope "/subscriptions/<subscription-id>"
```

In order to create the relevant resources within your Azure subscription, you'll need to register the resource provider.

```shell
az provider register --namespace Microsoft.ContainerInstance
```

At this point, I recommend running `az logout` to ensure your own Azure credentials are not being picked up as part of
the `DefaultAzureCredential` process.`DefaultAzureCredential` attempts to obtain a token from the following sources, in
the following (redacted) order:
- `EnvironmentCredential` - pulled from environment variables
- `VisualStudioCredential`
- `VisualStudioCodeCredential`
- `AzureCliCredential`
## Project Structure

### IM.API

The Infrastructure Management API provides a means for provisioning and managing the various infrastructure resources
within the cloud ecosystem. It includes Open API documentation, along with an Open API graphical interface for manually
provisioning resources and to provide client auto-generation capabilities. 

### IM.AppHost

The AppHost project is a .NET Aspire application that spins up the API project and all dependencies. It provides an
out-of-the-box dashboard that provides deeper insights into the application's behavior, including any telemetry
generated during operations.

### IM.API.Tests

This test project includes various unit tests to validate behavior of the application.

### IM.AppHost.Tests

This test project builds upon the main .NET Aspire project. In addition to spinning up the usual resources, it also
creates an OddDotNet container for automated testing of telemetry signals being produced.

## Design Considerations

### Subscriptions

The "default" subscription is used in this project, meaning whichever subscription is set as the default for the Service
Principal.

### Terraform

Typically, I would design this type of solution using Terraform, as that is generally safer and more in line with 
industry standards. However, in the interest of simplicity I decided to leave Terraform out of the equation and use
direct cloud provider SDKs instead. 

### CI/CD

I generally use pipelines for managing infrastructure, as this approach is also safer and more in line with industry
standards. As with Terraform, however, I decided against provisioning any sort of CI/CD pipelines.

### C# vs Golang

I'm much more familiar and comfortable with C#. While I've worked some with Golang - especially as it relates to 
protocol buffers and libraries like OpenTelemetry, my expertise is in the .NET stack, and for this assessment I can
move much quicker in that ecosystem. 

## Infrastructure Choices

I chose Azure for the cloud provider, although I've also used AWS and GCP in my career. I chose to create the following
resources:

### Resource Group

In Azure, resource groups are an abstract container used for holding any number of related resources. All infrastructure
resources are created and live within the context of a resource group, therefore creating this is the first step in
provisioning other resources.

### Virtual Network

The virtual network resource is required for provisioning a virtual machine in the next step. I thought creating and 
managing this as a separate step was important, as you can also manage things like public IP addresses through the vnet.

### Virtual Machine

The last resource for the demo is a virtual machine, created with the lowest tier available. It uses the virtual network
described above.

## OddDotNet

For full disclosure, I am the creator and maintainer of OddDotNet.

### What is it?

OddDotNet is a test harness for OpenTelemetry. It enables Observability Driven Development (hence the ODD in OddDotNet), 
which involves shifting left the validating and verifying of telemetry data that your application is producing. In this 
demo/assessment, I use OddDotNet to receive OpenTelemetry data from the API, and then I verify the telemetry using the 
query language I created, with the C# client that I provide via NuGet.