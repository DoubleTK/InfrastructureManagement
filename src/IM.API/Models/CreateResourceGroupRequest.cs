using System.ComponentModel.DataAnnotations;

namespace IM.API.Models;

public record CreateResourceGroupRequest
{
    [Required]
    public required string Location { get; init; }
    [Required]
    public required string Name { get; init; }
}