using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace IM.API.Services;

public static class ResourceGroupService
{
    /// <summary>
    /// Gets a resource group by name. The response is wrapped in a NullableResponse, as the resource group may not exist.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="resourceGroups"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A NullableResponse that may contain the ResourceGroupResource created as part of the request.</returns>
    internal static async Task<NullableResponse<ResourceGroupResource>> 
        GetResourceGroupAsync(string name, ResourceGroupCollection resourceGroups, CancellationToken cancellationToken = default)
     => await resourceGroups.GetIfExistsAsync(name, cancellationToken);

    /// <summary>
    /// Attempts to create a resource group within the default subscription. 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="location"></param>
    /// <param name="resourceGroups"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The ArmOperation representing the request to create the resource group.</returns>
    /// <exception cref="RequestFailedException">If the operation failed.</exception>
    internal static async Task<ArmOperation<ResourceGroupResource>> 
        CreateResourceGroupAsync(string name, AzureLocation location, ResourceGroupCollection resourceGroups, CancellationToken cancellationToken = default)
    {
        var data = new ResourceGroupData(location);
        return await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, name, data, cancellationToken);
    }
    
    internal static async Task<ArmOperation> DeleteResourceGroupAsync(ResourceGroupResource resourceGroup, CancellationToken cancellationToken = default)
        => await resourceGroup.DeleteAsync(WaitUntil.Started, cancellationToken: cancellationToken);
}