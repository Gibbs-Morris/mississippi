# Task 3.2: Create AppHost

**Status**: ⬜ Not Started  
**Depends On**: [3.1 Aspire Packages](./01-aspire-packages.md)

## Goal

Create `Cascade.AppHost` Aspire orchestration project that manages Azurite and Cosmos Emulator containers and wires connection strings to the Blazor Server.

## Acceptance Criteria

- [ ] `Cascade.AppHost` project created with Aspire SDK
- [ ] Azurite container resource added for blob storage
- [ ] Cosmos DB Emulator resource added for event store
- [ ] `Cascade.Server` project referenced with connection string injection
- [ ] Project builds with zero warnings
- [ ] Running `dotnet run` starts all resources and dashboard

## Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="13.1.0" />
  
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" />
    <PackageReference Include="Aspire.Hosting.Azure.CosmosDB" />
    <PackageReference Include="Aspire.Hosting.Azure.Storage" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Cascade.Server\Cascade.Server.csproj" />
  </ItemGroup>
</Project>
```

## Program.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");

// Add Cosmos DB (emulator)
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator()
    .AddDatabase("cascade-db");

// Add Cascade Server with resource references
builder.AddProject<Projects.Cascade_Server>("cascade-server")
    .WithReference(blobs)
    .WithReference(cosmos)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

## Connection String Resolution

In `Cascade.Server`, connection strings are available via:

```csharp
// Cosmos DB
builder.AddAzureCosmosClient("cosmos");

// Blob Storage
builder.AddAzureBlobClient("blobs");
```

Or via configuration:

```csharp
var cosmosConnection = builder.Configuration.GetConnectionString("cosmos");
var blobsConnection = builder.Configuration.GetConnectionString("blobs");
```

## TDD Steps

This task is infrastructure-only; validation is via running.

1. **Create**: Add project file and Program.cs
2. **Add to Solution**: Add to `samples.sln` and `samples.slnx`
3. **Build**: Verify zero warnings
4. **Run**: Start with `dotnet run` and verify dashboard appears

## Files to Create

- `samples/Cascade/Cascade.AppHost/Cascade.AppHost.csproj`
- `samples/Cascade/Cascade.AppHost/Program.cs`
- `samples/Cascade/Cascade.AppHost/Properties/launchSettings.json`

## launchSettings.json

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17171",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21171"
      }
    }
  }
}
```

## Notes

- Aspire dashboard shows resource health, logs, traces
- Emulators are container-based; Docker/Podman must be running
- First run may take time to pull container images
- Connection strings are injected automatically via Aspire's configuration system
