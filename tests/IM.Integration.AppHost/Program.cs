var builder = DistributedApplication.CreateBuilder(args);

// OddDotNet is the test harness for OpenTelemetry signals
var oddDotNet = builder
    .AddContainer("odddotnet", "ghcr.io/odddotnet/odddotnet")
    .WithImageTag("v0.4.1")
    .WithHttpEndpoint(targetPort: 4317, name: "grpc")
    .WithHttpEndpoint(targetPort: 4318, name: "http")
    .WithEnvironment("ODD_CACHE_EXPIRATION", "120000"); // Bump the cache timeout up since Azure operations take a while

// Add the main API project, and set it's OTel environment variable to send traces to OddDotNet
builder.AddProject<Projects.IM_API>("api")
    .WithEnvironment("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", oddDotNet.GetEndpoint("grpc"));

builder.Build().Run();