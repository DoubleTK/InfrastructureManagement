using System.Diagnostics;
using System.Net.Http.Json;
using Aspire.Hosting;
using Azure.Core;
using Grpc.Net.Client;
using IM.API.Models;
using OddDotCSharp;
using OddDotNet.Proto.Common.V1;
using OddDotNet.Proto.Trace.V1;

namespace IM.Integration.AppHost.Tests;

public class IntegrationTest : IAsyncLifetime
{
#pragma warning disable CS8618
    private DistributedApplication _app;
    private HttpClient _apiClient;
    private SpanQueryService.SpanQueryServiceClient _spanQueryServiceClient;
#pragma warning restore CS8618

    /// <summary>
    /// Steps for creating a Virtual Machine:
    /// 1. Create a resource group
    /// 2. Create a Virtual Network within the resource group
    /// 3. Create the Virtual Machine, including the NIC attached to the "Default" subnet of the vnet
    /// 4. Delete the entire resource group to clean everything up
    /// In addition, we send 'traceparent' headers for each request so that we can verify the OpenTelemetry signals
    /// are being generated as expected.
    /// </summary>
    [Fact]
    public async Task CreateVirtualMachine()
    {
        // Special OpenTelemetry header for passing in your own trace ID. The format is:
        // "00-byte[16]-byte[8]-01" a.k.a. ""00-traceId-spanId-01"
        const string traceParent = "traceparent";
        
        // Configure some constants for the resource names
        const string rgName = "rg-uipath-wus-001";
        const string vnetName = "vnet-uipath-wus-001";
        const string vmName = "vm-uipath-wus-001";
        
        // Configure the trace and span IDs we'll use to track each operation.
        var createRgTraceId = ActivityTraceId.CreateRandom();
        var createRgTraceIdAsBytes = Convert.FromHexString(createRgTraceId.ToHexString());
        var createRgSpanId = ActivitySpanId.CreateRandom();
        var createVnetTraceId = ActivityTraceId.CreateRandom();
        var createVnetTraceIdAsBytes = Convert.FromHexString(createVnetTraceId.ToHexString());
        var createVnetSpanId = ActivitySpanId.CreateRandom();
        var createVmTraceId = ActivityTraceId.CreateRandom();
        var createVmTraceIdAsBytes = Convert.FromHexString(createVmTraceId.ToHexString());
        var createVmSpanId = ActivitySpanId.CreateRandom();
        var deleteRgTraceId = ActivityTraceId.CreateRandom();
        var deleteRgTraceIdAsBytes = Convert.FromHexString(deleteRgTraceId.ToHexString());
        var deleteRgSpanId = ActivitySpanId.CreateRandom();
        
        await CreateAndValidateResourceGroup(rgName, traceParent, createRgTraceId, createRgSpanId, createRgTraceIdAsBytes);

        await CreateAndValidateVirtualNetwork(traceParent, createVnetTraceId, createVnetSpanId, rgName, createVnetTraceIdAsBytes);

        await CreateAndValidateVirtualMachine(vnetName, vmName, traceParent, createVmTraceId, createVmSpanId, rgName, createVmTraceIdAsBytes);

        await DeleteAndValidateResourceGroup(traceParent, deleteRgTraceId, deleteRgSpanId, rgName, deleteRgTraceIdAsBytes);
    }

    // Step 1: Create the resource group, validate we got a success, and confirm some OpenTelemetry signals were received
    private async Task CreateAndValidateResourceGroup(string name, string traceParent, ActivityTraceId createRgTraceId, ActivitySpanId createRgSpanId, byte[] createRgTraceIdAsBytes)
    {
        var createResourceGroupRequest = new CreateResourceGroupRequest
        {
            Name = name,
            Location = "westus"
        };
        
        // Configure the traceparent header using the create RG trace
        _apiClient.DefaultRequestHeaders.Add(traceParent, $"00-{createRgTraceId.ToString()}-{createRgSpanId.ToString()}-01");
        
        // Make the request
        var createResourceGroupHttpResponse = await _apiClient.PostAsJsonAsync("/azure/rg", createResourceGroupRequest);
        createResourceGroupHttpResponse.EnsureSuccessStatusCode();
        
        var createResourceGroupResponse = await createResourceGroupHttpResponse.Content.ReadFromJsonAsync<CreateResourceGroupResponse>();
        Assert.NotNull(createResourceGroupResponse);
        var rgIdentifier = new ResourceIdentifier(createResourceGroupResponse.Id);
        Assert.Equal(createResourceGroupRequest.Name, rgIdentifier.Name); // Make sure the identifier we got back has the correct name.
        
        // Build the query for the trace
        var createRgSpanQueryRequest = new SpanQueryRequestBuilder()
            .TakeAll()
            .Wait(TimeSpan.FromSeconds(1)) // Wait 1 second to make sure there aren't any straggler spans
            .Where(filters =>
            {
                filters.AddTraceIdFilter(createRgTraceIdAsBytes, ByteStringCompareAsType.Equals); // Find all spans with matching trace
            })
            .Build();
        
        // Make the query
        var createRgSpanQueryResponse = await _spanQueryServiceClient.QueryAsync(createRgSpanQueryRequest);
        
        Assert.NotEmpty(createRgSpanQueryResponse.Spans);
        Assert.Contains(createRgSpanQueryResponse.Spans, flatSpan => flatSpan.Span.Name == "POST Azure/rg"); // Make sure we hit the expected endpoint
    }
    
    // Step 2: Create the virtual network that the VM will use, validate we got a success, and confirm OTel signals
    private async Task CreateAndValidateVirtualNetwork(string traceParent, ActivityTraceId createVnetTraceId, ActivitySpanId createVnetSpanId, string rgName, byte[] createVnetTraceIdAsBytes)
    {
        var createVnetRequest = new CreateVirtualNetworkRequest
        {
            Name = "vnet-uipath-wus-001"
        };
        
        // Configure the traceparent for the vnet request
        _apiClient.DefaultRequestHeaders.Remove(traceParent);
        _apiClient.DefaultRequestHeaders.Add(traceParent, $"00-{createVnetTraceId.ToString()}-{createVnetSpanId.ToString()}-01");
        
        // make the request
        var createVnetHttpResponse = await _apiClient.PostAsJsonAsync($"/azure/rg/{rgName}/vnet", createVnetRequest);
        createVnetHttpResponse.EnsureSuccessStatusCode();

        var createVnetResponse = await createVnetHttpResponse.Content.ReadFromJsonAsync<CreateVirtualNetworkResponse>();
        Assert.NotNull(createVnetResponse);
        var vnetIdentifier = new ResourceIdentifier(createVnetResponse.Id);
        Assert.Equal(createVnetRequest.Name, vnetIdentifier.Name); // Make sure the identifier has the correct name
        
        // Build the query for the trace
        var createVnetSpanQueryRequest = new SpanQueryRequestBuilder()
            .TakeAll()
            .Wait(TimeSpan.FromSeconds(1)) // Wait 1 second to make sure there aren't any straggler spans
            .Where(filters =>
            {
                filters.AddTraceIdFilter(createVnetTraceIdAsBytes, ByteStringCompareAsType.Equals); // Find all spans that match the trace
            })
            .Build();
        
        // Make the query
        var createVnetSpanQueryResponse = await _spanQueryServiceClient.QueryAsync(createVnetSpanQueryRequest);
        
        Assert.NotEmpty(createVnetSpanQueryResponse.Spans);
        Assert.Contains(createVnetSpanQueryResponse.Spans, flatSpan => flatSpan.Span.Name == "POST Azure/rg/{rgName}/vnet");
    }
    
    // Step 3: Create and validate the virtual machine, ensure success, and validate OTel signals.
    private async Task CreateAndValidateVirtualMachine(string vnetName, string vmName, string traceParent, ActivityTraceId createVmTraceId, ActivitySpanId createVmSpanId, string rgName, byte[] createVmTraceIdAsBytes)
    {
        var createVmRequest = new CreateVirtualMachineRequest
        {
            VirtualNetworkName = vnetName,
            VirtualMachineName = vmName
        };
        
        // Configure the traceparent for the vm request
        _apiClient.DefaultRequestHeaders.Remove(traceParent);
        _apiClient.DefaultRequestHeaders.Add(traceParent, $"00-{createVmTraceId.ToString()}-{createVmSpanId.ToString()}-01");
        
        // Make the request
        var createVmHttpResponse = await _apiClient.PostAsJsonAsync($"/azure/rg/{rgName}/vm", createVmRequest);
        createVmHttpResponse.EnsureSuccessStatusCode();

        var createVmResponse = await createVmHttpResponse.Content.ReadFromJsonAsync<CreateVirtualMachineResponse>();
        Assert.NotNull(createVmResponse);
        var vmIdentifier = new ResourceIdentifier(createVmResponse.Id);
        Assert.Equal(createVmRequest.VirtualMachineName, vmIdentifier.Name); // Make sure the identifier has the correct name
        
        // Build the query for the trace
        var createVmSpanQueryRequest = new SpanQueryRequestBuilder()
            .TakeAll()
            .Wait(TimeSpan.FromSeconds(1)) // Wait for 1 second to catch any straggler spans
            .Where(filters =>
            {
                filters.AddTraceIdFilter(createVmTraceIdAsBytes, ByteStringCompareAsType.Equals);
            })
            .Build();
        
        // Make the query
        var createVmSpanQueryResponse = await _spanQueryServiceClient.QueryAsync(createVmSpanQueryRequest);
        
        Assert.NotEmpty(createVmSpanQueryResponse.Spans);
        Assert.Contains(createVmSpanQueryResponse.Spans, flatSpan => flatSpan.Span.Name == "POST Azure/rg/{rgName}/vm");
    }
    
    // Step 4: Cleanup the resource group and contents, validate we got a success, and confirm some OpenTelemetry signals were received
    private async Task DeleteAndValidateResourceGroup(string traceParent, ActivityTraceId deleteRgTraceId, ActivitySpanId deleteRgSpanId, string rgName, byte[] deleteRgTraceIdAsBytes)
    {
        _apiClient.DefaultRequestHeaders.Remove(traceParent);
        _apiClient.DefaultRequestHeaders.Add(traceParent, $"00-{deleteRgTraceId.ToString()}-{deleteRgSpanId.ToString()}-01");

        var deleteResourceGroupHttpResponse = await _apiClient.DeleteAsync($"/azure/rg/{rgName}");
        deleteResourceGroupHttpResponse.EnsureSuccessStatusCode();

        var deleteRgSpanQueryRequest = new SpanQueryRequestBuilder()
            .TakeAll()
            .Wait(TimeSpan.FromSeconds(1))
            .Where(filters =>
            {
                filters.AddTraceIdFilter(deleteRgTraceIdAsBytes, ByteStringCompareAsType.Equals);
            })
            .Build();

        var deleteRgSpanQueryResponse = await _spanQueryServiceClient.QueryAsync(deleteRgSpanQueryRequest);
        
        Assert.NotEmpty(deleteRgSpanQueryResponse.Spans);
        Assert.Contains(deleteRgSpanQueryResponse.Spans, flatSpan => flatSpan.Span.Name == "DELETE Azure/rg/{rgName}");
    }

    /// <summary>
    /// Configure the HTTP and gRPC clients for communicating with the API and for receiving/querying telemetry data
    /// </summary>
    public async Task InitializeAsync()
    {
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.IM_Integration_AppHost>();
        _app = await appHostBuilder.BuildAsync();
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        await _app.StartAsync();
        _apiClient = _app.CreateHttpClient("api");
        await resourceNotificationService.WaitForResourceAsync("api", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        await resourceNotificationService.WaitForResourceAsync("odddotnet", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var channel = GrpcChannel.ForAddress(_app.GetEndpoint("odddotnet", "grpc"));
        _spanQueryServiceClient = new SpanQueryService.SpanQueryServiceClient(channel);
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}