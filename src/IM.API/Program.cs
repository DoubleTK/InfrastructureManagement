using Azure.Identity;
using Azure.ResourceManager;
using IM.API.Services;
using IM.ServiceDefaults;

const string azureTenantId = "AZURE_TENANT_ID";
const string azureClientId = "AZURE_CLIENT_ID";
const string azureClientSecret = "AZURE_CLIENT_SECRET";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddServiceDefaults();

// If user secrets are being used, we want to pull the credentials from there and set the appropriate environment variables,
// assuming that those env vars don't already exist.
if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable(azureTenantId)) && !String.IsNullOrEmpty(builder.Configuration[azureTenantId]))
{
    Environment.SetEnvironmentVariable(azureTenantId, builder.Configuration[azureTenantId]);
}

if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable(azureClientId)) && !String.IsNullOrEmpty(builder.Configuration[azureClientId]))
{
    Environment.SetEnvironmentVariable(azureClientId, builder.Configuration[azureClientId]);
}

if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable(azureClientSecret)) && !String.IsNullOrEmpty(builder.Configuration[azureClientSecret]))
{
    Environment.SetEnvironmentVariable(azureClientSecret, builder.Configuration[azureClientSecret]);
}


// Client for managing Azure resources. This uses the DefaultAzureCredential, which pulls in the corresponding
// environment variables (see the README for more info) necessary for authenticating against Azure.
builder.Services.AddScoped<ArmClient>(_ => new ArmClient(new DefaultAzureCredential()));

// Our wrapper around the ArmClient functionality for provisioning resources.
builder.Services.AddScoped<AzureService>();

var app = builder.Build();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();