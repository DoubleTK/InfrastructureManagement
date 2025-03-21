using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;

namespace IM.API.Services;

public static class VirtualNetworkService
{
    internal static async Task<NullableResponse<VirtualNetworkResource>> 
        GetVirtualNetworkAsync(ResourceGroupResource resourceGroup, string name, CancellationToken cancellationToken = default)
            => await resourceGroup.GetVirtualNetworks().GetIfExistsAsync(name, cancellationToken: cancellationToken);

    internal static async Task<ArmOperation<VirtualNetworkResource>> 
        CreateVirtualNetworkAsync(ResourceGroupResource resourceGroup, string name, CancellationToken cancellationToken = default)
    {
        var data = new VirtualNetworkData
        {
            Location = resourceGroup.Data.Location,
            AddressPrefixes = { "10.0.0.0/16" },
            Subnets =
            {
                new SubnetData
                {
                    AddressPrefix = "10.0.0.0/24",
                    Name = "Default"
                }
            }
        };
        
        return await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, name, data, cancellationToken);
    }
}