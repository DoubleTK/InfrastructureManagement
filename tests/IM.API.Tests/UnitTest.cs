using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using IM.API.Controllers;
using IM.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace IM.API.Tests;

public class UnitTest
{
    [Fact]
    public async Task InvalidCredentials_ReturnsUnauthorized()
    {
        Mock<ArmClient> mockArmClient = new();
        var requestFailedException = new RequestFailedException(401, "Unauthorized");
        var logger = new Mock<ILogger<AzureController>>();
        mockArmClient.Setup(client => client.GetDefaultSubscriptionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(requestFailedException);
        var azureService = new AzureService(mockArmClient.Object);
        var azureController = new AzureController(logger.Object, azureService);

        var response = await azureController.GetResourceGroupAsync("test");
        Assert.IsType<UnauthorizedHttpResult>(response.Result);
    }

    [Fact]
    public async Task MissingResourceGroup_ReturnsNotFound()
    {
        var armClientMock = new Mock<ArmClient>();
        var loggerMock = new Mock<ILogger<AzureController>>();
        var subscriptionMock = new Mock<SubscriptionResource>();
        var resourceGroupsMock = new Mock<ResourceGroupCollection>();
        var nullableResponseMock = new Mock<NullableResponse<ResourceGroupResource>>();
        nullableResponseMock.Setup(nullableResponse => nullableResponse.HasValue) // Sets up the missing resource group
            .Returns(false);
        resourceGroupsMock.Setup(resourceGroups => resourceGroups.GetIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nullableResponseMock.Object);
        subscriptionMock.Setup(subscription => subscription.GetResourceGroups())
            .Returns(resourceGroupsMock.Object);
        armClientMock.Setup(client => client.GetDefaultSubscriptionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptionMock.Object);
        
        var azureService = new AzureService(armClientMock.Object);
        var azureController = new AzureController(loggerMock.Object, azureService);
        
        var response = await azureController.GetResourceGroupAsync("test");
        Assert.IsType<NotFound>(response.Result);
    }
}