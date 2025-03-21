using System.ComponentModel.DataAnnotations;

namespace IM.API.Models;

public record CreateVirtualMachineRequest
{
    [Required]
    public required string VirtualMachineName { get; init; }
    
    [Required]
    public required string VirtualNetworkName { get; init; }
}