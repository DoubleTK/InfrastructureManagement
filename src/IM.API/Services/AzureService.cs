using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;

namespace IM.API.Services;

public class AzureService
{
    private readonly ArmClient _armClient;

    public AzureService(ArmClient armClient)
    {
        _armClient = armClient;
    }

    public async Task<NullableResponse<ResourceGroupResource>> GetResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync(cancellationToken);
        var resourceGroups = subscription.GetResourceGroups();
        return await ResourceGroupService.GetResourceGroupAsync(resourceGroupName, resourceGroups, cancellationToken);
    }

    public async Task<ArmOperation<ResourceGroupResource>> CreateResourceGroupAsync(string resourceGroupName, AzureLocation location, CancellationToken cancellationToken = default)
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync(cancellationToken);
        var resourcesGroups = subscription.GetResourceGroups();
        return await ResourceGroupService.CreateResourceGroupAsync(resourceGroupName, location, resourcesGroups, cancellationToken);
    }

    public async Task<ArmOperation> DeleteResourceGroupAsync(ResourceGroupResource resourceGroup, CancellationToken cancellationToken = default)
        => await ResourceGroupService.DeleteResourceGroupAsync(resourceGroup, cancellationToken);

    public async Task<NullableResponse<VirtualNetworkResource>> GetVirtualNetworkAsync(ResourceGroupResource resourceGroup, string vnetName, CancellationToken cancellationToken = default)
        => await VirtualNetworkService.GetVirtualNetworkAsync(resourceGroup, vnetName, cancellationToken);

    public async Task<ArmOperation<VirtualNetworkResource>> CreateVirtualNetworkAsync(ResourceGroupResource resourceGroup, string vnetName, CancellationToken cancellationToken = default)
        => await VirtualNetworkService.CreateVirtualNetworkAsync(resourceGroup, vnetName, cancellationToken);

    public async Task<NullableResponse<VirtualMachineResource>> GetVirtualMachineAsync(ResourceGroupResource resourceGroup, string virtualMachineName, CancellationToken cancellationToken = default)
        => await VirtualMachineService.GetVirtualMachineAsync(resourceGroup, virtualMachineName, cancellationToken);

    public async Task<ArmOperation<VirtualMachineResource>> 
        CreateVirtualMachineAsync(ResourceGroupResource resourceGroup, VirtualNetworkResource vnet, string virtualMachineName, CancellationToken cancellationToken = default)
        => await VirtualMachineService.CreateVirtualMachineAsync(resourceGroup, vnet, virtualMachineName, cancellationToken);
}