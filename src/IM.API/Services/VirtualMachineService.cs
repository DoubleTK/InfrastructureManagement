using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;

namespace IM.API.Services;

public static class VirtualMachineService
{
    internal static async Task<NullableResponse<VirtualMachineResource>> 
        GetVirtualMachineAsync(ResourceGroupResource resourceGroup, string name, CancellationToken cancellationToken = default)
        => await resourceGroup.GetVirtualMachines().GetIfExistsAsync(name, cancellationToken: cancellationToken);

    internal static async Task<ArmOperation<VirtualMachineResource>> 
        CreateVirtualMachineAsync(ResourceGroupResource rg, VirtualNetworkResource vnet, string name, CancellationToken cancellationToken = default)
    {
        var nicOperation = await CreateNetworkInterfaceAsync(rg, vnet, name, cancellationToken);
        var data = GetVirtualMachineData(rg, nicOperation.Value, name);
        return await rg.GetVirtualMachines().CreateOrUpdateAsync(WaitUntil.Completed, name, data, cancellationToken);
    }

    private static async Task<ArmOperation<NetworkInterfaceResource>> 
        CreateNetworkInterfaceAsync(ResourceGroupResource rg, VirtualNetworkResource vnet, string name, CancellationToken cancellationToken = default)
    {
        var nics = rg.GetNetworkInterfaces();
        var nic = new NetworkInterfaceData
        {
            Location = rg.Data.Location,
            IPConfigurations =
            {
                new NetworkInterfaceIPConfigurationData
                {
                    Name = $"{name}-nic-config",
                    Subnet = new SubnetData
                    {
                        Id = vnet.Data.Subnets.First().Id
                    },
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
                }
            }
        };
        
        return await nics.CreateOrUpdateAsync(WaitUntil.Completed, $"{name}-nic", nic, cancellationToken);
    }

    private static VirtualMachineData GetVirtualMachineData(ResourceGroupResource rg, NetworkInterfaceResource nic, string name)
    {
        var data = new VirtualMachineData(rg.Data.Location)
        {
            Location = rg.Data.Location,
            HardwareProfile = new VirtualMachineHardwareProfile
            {
                VmSize = VirtualMachineSizeType.StandardB1S
            },
            StorageProfile = new VirtualMachineStorageProfile
            {
                OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                {
                    ManagedDisk = new VirtualMachineManagedDisk
                    {
                        StorageAccountType = StorageAccountType.StandardLrs
                    },
                    DeleteOption = DiskDeleteOptionType.Delete
                },
                ImageReference = new ImageReference
                {
                    Publisher = "canonical",
                    Offer = "ubuntu-24_04-lts",
                    Sku = "server",
                    Version = "latest"
                }
            },
            SecurityProfile = new SecurityProfile
            {
                SecurityType = SecurityType.TrustedLaunch,
                UefiSettings = new UefiSettings
                {
                    IsSecureBootEnabled = true,
                    IsVirtualTpmEnabled = true
                }
            },
            AdditionalCapabilities = new AdditionalCapabilities
            {
                HibernationEnabled = false
            },
            OSProfile = new VirtualMachineOSProfile
            {
                ComputerName = name,
                AdminUsername = "UIPathAdmin",
                AdminPassword = "Password123!",
                LinuxConfiguration = new LinuxConfiguration
                {
                    PatchSettings = new LinuxPatchSettings
                    {
                        AssessmentMode = LinuxPatchAssessmentMode.ImageDefault,
                        PatchMode = LinuxVmGuestPatchMode.ImageDefault
                    },

                }
            },
            NetworkProfile = new VirtualMachineNetworkProfile
            {
                NetworkInterfaces =
                {
                    new VirtualMachineNetworkInterfaceReference
                    {
                        Id = nic.Data.Id,
                        DeleteOption = ComputeDeleteOption.Delete
                    }
                }
            }
        };

        return data;
    }
}