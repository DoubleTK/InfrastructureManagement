var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.IM_API>("api");

builder.Build().Run();