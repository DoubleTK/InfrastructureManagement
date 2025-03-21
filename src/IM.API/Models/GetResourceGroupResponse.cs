namespace IM.API.Models;

public record GetResourceGroupResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Location { get; init; }
    public required string State { get; init; }
}