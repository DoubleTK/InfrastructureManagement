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
- Free (or higher) tier Azure account.
- The Azure CLI, for generating credentials to access Azure resources. Installation instructions for the various OS's
located [here](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).

## Requirements

Per the take-home assessment documentation, the following functionality will be provided:

### Provisioning of Cloud Resources

This demo project creates the following resources in Azure: 

#### Resource Group
The resource group is the logical container for holding the other Azure resources.

#### Virtual Network
A virtual network is created for later use by the virtual machine. Most values are hardcoded, including an address space
of "10.0.0.0/16", and a single subnet called "Default" with an address space of "10.0.0.0/24".

#### Virtual Machine
Most values for the virtual machine are hardcoded. The VM uses the latest Ubuntu 24.04 LTS release, with the smallest
VM size available to minimize costs for the demo. In addition, a network interface is created and associated with the
first subnet found attached to the virtual network, which is just the "Default" subnet created in the virtual network
step.

### Teardown of Cloud Resources
In addition to the ability to create and query cloud resources, I've also included a DELETE option which is exercised
by the single integration test I wrote to ensure cloud resources are not left around after a test has completed. 

## Bonus Requirements
In addition to the basic requirements of managing cloud resources, I've also included the following:

### Unit and Integration Tests
I've included two unit tests to verify failure case functionality of the resource group endpoint. I'm a strong advocate
for TDD and good coverage of all functionality, but in the interest of time and getting the demo submitted sooner rather
than later, I only included two unit tests. The two tests check for NotFound and Unauthorized responses from the `ArmClient`,
which demonstrates the ability to mock out interactions with external services and still verify the behavior of internal
code. 

In addition to the two unit tests, I've also included an integration test that runs through the full process of creating
a virtual machine and supporting infrastructure. The integration test creates a resource group, a virtual network with 
subnet, and a virtual machine attached to that subnet, and then deletes the entire group at the end of the test.

I also test the telemetry data that is being produced as part of this process, using a tool I developed and maintain
called OddDotNet (see more below). I create a trace ID, execute the workflow, and then obtain all spans associated with
that trace ID, subsequently verifying that the correct spans are present. 

### Logging and Error Handling
For this project, I am using a relatively new tool called .NET Aspire. This tool provides a number of features out of the 
box, including a dashboard with log, trace, and metric telemetry. For more information, see the project descriptions
of `IM.AppHost` and `IM.Integration.AppHost`. 

All API endpoints are documented with OpenAPI specs, and return appropriate status codes based on the results of the
operation. A Swagger endpoint can be found at the `"https://{host}:{port}/swagger` url. 

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
> Alternatively, if you are using an IDE such as Rider, Visual Studio, or vscode, these include functionality for opening
> and modifying the user secrets within the IDE without having to search for the directory.

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
application startup and used by the `DefaultAzureCredential` provider to enable access to Azure resources. Alternatively,
you may set these environment variables on your local machine without having to use the user secrets file if you desire.

Typically, I would add this `InfrastructureManagement` Service Principal to a group and manage permissions that way. However,
to keep things simple we will assign the appropriate permissions directly to the SP rather than to a group the SP belongs to.

For simplicity, we'll be assigning the "Contributor" role to the SP. A more granular role(s) may be desired in a production
environment, but for simplicity we'll stick with Contributor. You'll need the subscription ID to make this happen:

```shell
az account list
```

You'll also need the object ID of the SP for this assignment:

```shell
az ad sp list \
  --filter "startswith(displayName, 'InfrastructureManagement')" \
  --query "[].{objectId:id, displayName:displayName}"
```

After obtaining the subscription ID (the one selected as active), assign the role to the SP (replace <object-id> with 
the InfrastructureManagement object ID, and <subscription-id> with your subscription ID):

```shell
az role assignment create --assignee "<object-id>" \
  --role "Contributor" \
  --scope "/subscriptions/<subscription-id>"
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

I don't recommend running this project directly, although you can. The `IM.AppHost` project will run this API and provide
a number of additional features, such as OpenTelemetry configuration, service discovery, HTTP resilience, and a
comprehensive dashboard for viewing telemetry data of the applicaiton while it's running.

### IM.AppHost

The AppHost project is a .NET Aspire application that spins up the API project and all dependencies. It provides an
out-of-the-box dashboard that includes deeper insights into the application's behavior, including any telemetry
generated during operations.

I recommend running this project as the entry point. To do so, simply CD into the directory where `IM.AppHost.csproj`
lives and run `dotnet run`.

Upon execution, a link to the dashboard will be provided in the console. Navigating to this dashboard will provide you
with additional information related to the running API, such as telemetry. You may also click on the `https` link in
the dashboard for the `IM.API` project, and then navigate to the `/swagger` endpoint to open the SwaggerUI (OpenAPI). 

When executing the various Swagger endpoints, you may take a look at the Traces tab in the dashboard to see the full
OpenTelemetry trace of the operation, or take a look at the Metrics tab to view metrics associated with the API.

### IM.ServiceDefaults
This project can be ignored, unless you're interested in seeing how the configuration of OpenTelemetry and other API 
requirements are set up. This is a mostly-boilerplate project intended for use as part of the .NET Aspire workload. 

### IM.API.Tests

This test project includes two unit tests to validate behavior of the application. They live in a generic `UnitTest` class,
which of course is not best practice, but I kept it simple for the sake of the demo.

### IM.Integration.AppHost
The integration AppHost project is very similar to the main `IM.AppHost` project, except it is intended to be used by the
integration test rather than as part of developer workflow. In addition to configuring the `IM.API` to start up, it also
pulls in the OddDotNet container and provides the container's gRPC endpoint to the `IM.API` as an environment variable so
that the API can send its telemetry to OddDotNet for telemetry testing. 

### IM.Integration.AppHost.Tests

This test project using the `IM.Integration.AppHost` project to spin up the `DistributedApplication` for testing. A single
integration test is provide for the demo, with a resource group, virtual network, and virtual machine being created. It 
also validates various OpenTelemetry spans, ensuring they are present as part of the operation, and it deletes the entire
resource group and infrastructure as part of the test to ensure no resources are left in the cloud upon test completion.

## Design Considerations

### Subscriptions

The "default" subscription is used in this project, meaning whichever subscription is set as the default for the Service
Principal.

### Terraform

Typically, I would design this type of solution using Terraform, as I usually prefer a more declarative approach to 
infrastructure management. The Terraform code would be packaged up as part of a build pipeline, and deployed to the various
environments as part of a CI/CD process. However, taking inspiration from the "novelty" criteria in the assessment, I 
decided to use the Azure Resource Manager client SDK (`ArmClient`), which is a tool I've never used before, so I thought
it'd be interesting to learn a new SDK while working with cloud resources that I was already familiar with through Terraform.

### C# vs Golang

I'm much more familiar and comfortable with C#. While I've worked some with Golang - especially as it relates to 
protocol buffers and libraries like OpenTelemetry, my expertise is in the .NET stack, and for this assessment I can
move much quicker in that ecosystem. I would love to learn Golang, as it has been on my list of new languages to pick up,
but for the sake of speed for this assessment I chose C#. 

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

If you'd like to learn more about OddDotNet, I have a website located [here](https://odddotnet.github.io/OddDotDocs/). I also
have a couple examples of how to use OddDotNet in your own workflows on my YouTube channel [here](https://www.youtube.com/@Tyler-Kenna/videos).

## Conclusion
Thank you for taking the time to review my take home assessment. If you have any questions or run into issues, please
don't hesitate to reach out. 
