using System.ComponentModel.DataAnnotations;

namespace IM.API.Models;

public record CreateVirtualNetworkRequest
{
    [Required]
    public required string Name { get; init; }
}