using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;

namespace IM.API.Services;

/// <summary>
/// Public facing service for communicating with various Azure resources. 
/// </summary>
public class AzureService
{
    private readonly ArmClient _armClient;

    public AzureService(ArmClient armClient)
    {
        _armClient = armClient;
    }

    /// <summary>
    /// Returns the specified resource group as part of a nullable response, or none if the resource group or
    /// default subscription does not exist.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to find.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A NullableResponse containing the resource group, or none if it doesn't exist.</returns>
    public async Task<NullableResponse<ResourceGroupResource>> GetResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync(cancellationToken);
        var resourceGroups = subscription.GetResourceGroups();
        return await ResourceGroupService.GetResourceGroupAsync(resourceGroupName, resourceGroups, cancellationToken);
    }

    /// <summary>
    /// Create a resource group with a given name and location in the default subscription.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to create.</param>
    /// <param name="location">The Azure location where the resource group should be created.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The ArmOperation describing this request. See <see cref="ArmOperation"/> for more details.</returns>
    public async Task<ArmOperation<ResourceGroupResource>> CreateResourceGroupAsync(string resourceGroupName, AzureLocation location, CancellationToken cancellationToken = default)
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync(cancellationToken);
        var resourcesGroups = subscription.GetResourceGroups();
        return await ResourceGroupService.CreateResourceGroupAsync(resourceGroupName, location, resourcesGroups, cancellationToken);
    }

    /// <summary>
    /// Deletes the resource group specified by the <see cref="ResourceGroupResource"/>.
    /// </summary>
    /// <param name="resourceGroup">The <see cref="ResourceGroupResource"/> to delete.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ArmOperation"/> representing the results of the request.</returns>
    public async Task<ArmOperation> DeleteResourceGroupAsync(ResourceGroupResource resourceGroup, CancellationToken cancellationToken = default)
        => await ResourceGroupService.DeleteResourceGroupAsync(resourceGroup, cancellationToken);

    /// <summary>
    /// Gets the requested virtual network with the specified resource group, or none if it doesn't exist.
    /// </summary>
    /// <param name="resourceGroup">The <see cref="ResourceGroupResource"/> where the vnet lives.</param>
    /// <param name="vnetName">The name of the virtual network.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The virtual network, or none if it doesn't exist.</returns>
    public async Task<NullableResponse<VirtualNetworkResource>> GetVirtualNetworkAsync(ResourceGroupResource resourceGroup, string vnetName, CancellationToken cancellationToken = default)
        => await VirtualNetworkService.GetVirtualNetworkAsync(resourceGroup, vnetName, cancellationToken);

    /// <summary>
    /// Creates a new virtual network within a resource group, in the same location. The address prefix for this virtual
    /// network is hard coded to 10.0.0.0/16, with a single subnet named "Default" at 10.0.0.0/24. 
    /// </summary>
    /// <param name="resourceGroup">The resource group where the virtual network should be created.</param>
    /// <param name="vnetName">The name of the virtual network.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ArmOperation"/> representing the results of the request.</returns>
    public async Task<ArmOperation<VirtualNetworkResource>> CreateVirtualNetworkAsync(ResourceGroupResource resourceGroup, string vnetName, CancellationToken cancellationToken = default)
        => await VirtualNetworkService.CreateVirtualNetworkAsync(resourceGroup, vnetName, cancellationToken);

    /// <summary>
    /// Gets the requested virtual machine within the specified resource group, or none if it doesn't exist.
    /// </summary>
    /// <param name="resourceGroup">The <see cref="ResourceGroupResource"/> where the vm lives.</param>
    /// <param name="virtualMachineName">The name of the virtual machine.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The virtual machine, or none if it doesn't exist.</returns>
    public async Task<NullableResponse<VirtualMachineResource>> GetVirtualMachineAsync(ResourceGroupResource resourceGroup, string virtualMachineName, CancellationToken cancellationToken = default)
        => await VirtualMachineService.GetVirtualMachineAsync(resourceGroup, virtualMachineName, cancellationToken);

    /// <summary>
    /// Creates a new virtual machine within a resource group, in the same location. Most values of the virtual machine
    /// are hardcoded:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Setting</description>
    ///     </listheader>
    ///     <item>
    ///         <term>VMSize</term>
    ///         <description>StandardB1S</description>
    ///     </item>
    ///     <item>
    ///         <term>OS Storage</term>
    ///         <description>StandardLRS</description>
    ///     </item>
    ///     <item>
    ///         <term>Image</term>
    ///         <description>Ubuntu 24.04 LTS</description>
    ///     </item>
    ///     <item>
    ///         <term>Admin Username</term>
    ///         <description>UIPathAdmin</description>
    ///     </item>
    ///     <item>
    ///         <term>Admin Password</term>
    ///         <description>Password123!</description>
    ///     </item>
    /// </list>
    ///
    /// Obviously, things like the password should not be stored here, and should not be hard coded like this. Using
    /// an SSH key is likely the safer way to go.
    ///
    /// In addition to the virtual machine, a network interface is also created and associated with the VM. The NIC is
    /// attached to the "Default" subnet of the virtual network, with a dynamic IP provisioned from the 10.0.0.0/24 address
    /// space.
    /// </summary>
    /// <param name="resourceGroup">The resource group where the virtual machine should be created.</param>
    /// <param name="vnet">The virtual network that this virtual machine's NIC should be attached to.</param>
    /// <param name="virtualMachineName">The name of the virtual machine.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="ArmOperation"/> representing the results of the request.</returns>
    public async Task<ArmOperation<VirtualMachineResource>> 
        CreateVirtualMachineAsync(ResourceGroupResource resourceGroup, VirtualNetworkResource vnet, string virtualMachineName, CancellationToken cancellationToken = default)
        => await VirtualMachineService.CreateVirtualMachineAsync(resourceGroup, vnet, virtualMachineName, cancellationToken);
}