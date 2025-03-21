using Azure;
using Azure.ResourceManager.Resources;
using IM.API.Models;
using IM.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace IM.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AzureController
{
    private readonly ILogger<AzureController> _logger;
    private readonly AzureService _azureService;

    public AzureController(ILogger<AzureController> logger, AzureService azureService)
    {
        _logger = logger;
        _azureService = azureService;
    }

    [HttpPost("rg")]
    
    [ProducesResponseType<CreateResourceGroupResponse>(201, "application/json")]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType<ProblemDetails>(409, "application/json")]
    public async Task<Results<Created<CreateResourceGroupResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, Conflict<ProblemDetails>>>
        CreateResourceGroup(CreateResourceGroupRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var operation = await _azureService.CreateResourceGroupAsync(request.Name, request.Location, cancellationToken);
            var resourceGroup = operation.Value;

            var response = new CreateResourceGroupResponse
            {
                Id = resourceGroup.Id.ToString(),
                Location = resourceGroup.Data.Location.Name,
                Name = resourceGroup.Data.Name,
                State = resourceGroup.Data.ResourceGroupProvisioningState
            };
            return TypedResults.Created(resourceGroup.Id.ToString(), response);
        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            return GetConflictExceptionResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }

    [HttpGet("rg/{name}")]
    [ProducesResponseType<GetResourceGroupResponse>(200, "application/json")]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<Results<Ok<GetResourceGroupResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, NotFound>> 
        GetResourceGroupAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var rgResponse = await _azureService.GetResourceGroupAsync(name, cancellationToken);
            
            if (!rgResponse.HasValue) return TypedResults.NotFound();
            
            ResourceGroupData rg = rgResponse.Value!.Data;
            var response = new GetResourceGroupResponse
            {
                Location = rg.Location.Name,
                Id = rg.Id.ToString(),
                Name = rg.Name,
                State = rg.ResourceGroupProvisioningState
            };
            return TypedResults.Ok(response);

        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }

    [HttpDelete("rg/{name}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<Results<NoContent, BadRequest<ProblemDetails>, UnauthorizedHttpResult, NotFound>>
        DeleteResourceGroupAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
           var rgResponse = await _azureService.GetResourceGroupAsync(name, cancellationToken);
           
            if (!rgResponse.HasValue) return TypedResults.NotFound();
            
            await _azureService.DeleteResourceGroupAsync(rgResponse.Value!, cancellationToken);
            
            return TypedResults.NoContent();
        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }
    
    [HttpPost("rg/{rgName}/vm")]
    [ProducesResponseType<CreateVirtualMachineResponse>(201, "application/json")]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType<ProblemDetails>(409, "application/json")]
    public async Task<Results<Created<CreateVirtualMachineResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, NotFound, Conflict<ProblemDetails>>>
        CreateVirtualMachineAsync(string rgName, CreateVirtualMachineRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var rgResponse = await _azureService.GetResourceGroupAsync(rgName, cancellationToken);
            
            if (!rgResponse.HasValue) return TypedResults.NotFound();

            var rg = rgResponse.Value!;
            var vnetResponse = await _azureService.GetVirtualNetworkAsync(rg, request.VirtualNetworkName, cancellationToken);
            
            if (!vnetResponse.HasValue) return TypedResults.NotFound();

            var vnet = vnetResponse.Value!;

            var operation = await _azureService.CreateVirtualMachineAsync(rg, vnet, request.VirtualMachineName, cancellationToken);

            var response = new CreateVirtualMachineResponse(operation.Value.Id.ToString());
            
            return TypedResults.Created(response.Id, response);
        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            return GetConflictExceptionResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }

    [HttpGet("rg/{rgName}/vm/{vmName}")]
    [ProducesResponseType<GetVirtualMachineResponse>(200, "application/json")]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<Results<Ok<GetVirtualMachineResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, NotFound>>
        GetVirtualMachineAsync(string rgName, string vmName, CancellationToken cancellationToken = default)
    {
        try
        {
            var rgResponse = await _azureService.GetResourceGroupAsync(rgName, cancellationToken);
            
            if (!rgResponse.HasValue) return TypedResults.NotFound();
            
            var vmResponse = await _azureService.GetVirtualMachineAsync(rgResponse.Value!, vmName, cancellationToken);

            if (!vmResponse.HasValue || !vmResponse.Value!.HasData) return TypedResults.NotFound();
            
            var vm = vmResponse.Value!;

            var response = new GetVirtualMachineResponse(vm.Id.ToString(), vm.Data.Location.ToString());

            return TypedResults.Ok(response);
        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }

    [HttpPost("rg/{rgName}/vnet")]
    [ProducesResponseType<CreateVirtualNetworkResponse>(201, "application/json")]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType<ProblemDetails>(409, "application/json")]
    public async Task<Results<Created<CreateVirtualNetworkResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, NotFound, Conflict<ProblemDetails>>>
        CreateVirtualNetworkAsync(string rgName, CreateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var rgResponse = await _azureService.GetResourceGroupAsync(rgName, cancellationToken);

            if (!rgResponse.HasValue) return TypedResults.NotFound();

            var operation = await _azureService.CreateVirtualNetworkAsync(rgResponse.Value!, request.Name, cancellationToken);

            var response = new CreateVirtualNetworkResponse(operation.Value.Id.ToString(), operation.Value.Data.Location?.ToString() ?? "");

            return TypedResults.Created(response.Id, response);
        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            return GetConflictExceptionResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }

    [HttpGet("rg/{rgName}/vnet/{vnetName}")]
    [ProducesResponseType<GetVirtualNetworkResponse>(200, "application/json")]
    [ProducesResponseType<ProblemDetails>(400, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<Results<Ok<GetVirtualNetworkResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, NotFound>> 
        GetVirtualNetworkAsync(string rgName, string vnetName, CancellationToken cancellationToken = default)
    {
        try
        {
            var rgResponse = await _azureService.GetResourceGroupAsync(rgName, cancellationToken);
            
            if (!rgResponse.HasValue) return TypedResults.NotFound();
            
            var vnetResponse = await _azureService.GetVirtualNetworkAsync(rgResponse.Value!, vnetName, cancellationToken);
            
            if (!vnetResponse.HasValue) return TypedResults.NotFound();

            var response = new GetVirtualNetworkResponse(vnetResponse.Value!.Id.ToString(), vnetResponse.Value!.Data.Location?.ToString() ?? "");
            
            return TypedResults.Ok(response);
        }
        catch (RequestFailedException e) when (e.Status == 401)
        {
            return GetUnauthorizedResponse(e);
        }
        catch (Exception e)
        {
            return GetGenericExceptionResponse(e);
        }
    }

    private BadRequest<ProblemDetails> GetGenericExceptionResponse(Exception e)
    {
        _logger.LogError(e, "Unexpected exception");
        var problemDetails = new ProblemDetails
        {
            Detail = e.Message,
        };
        return TypedResults.BadRequest(problemDetails);
    }

    private Conflict<ProblemDetails> GetConflictExceptionResponse(RequestFailedException e)
    {
        _logger.LogError(e, "The requested update encountered a conflict");
        var problemDetails = new ProblemDetails
        {
            Detail = e.Message,
        };
        return TypedResults.Conflict(problemDetails);
    }

    private UnauthorizedHttpResult GetUnauthorizedResponse(RequestFailedException e)
    {
        _logger.LogError(e, "Unauthorized");
        return TypedResults.Unauthorized();
    }
}