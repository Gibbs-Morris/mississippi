# Task 3.3: Create Blazor Server

**Status**: ⬜ Not Started  
**Depends On**: [3.2 AppHost](./02-apphost.md), Phase 1 (SignalR integration), Phase 2 (Domain)

## Goal

Create `Cascade.Server` project hosting Blazor Server with cohosted Orleans silo, SignalR hub, and domain registration.

## Acceptance Criteria

- [ ] Blazor Server project with Interactive Server rendering
- [ ] Orleans silo cohosted using `UseOrleans`
- [ ] SignalR hub mapped for real-time projections
- [ ] Domain registered via `AddCascadeDomain()`
- [ ] Cosmos/Azurite connections wired from Aspire
- [ ] Basic layout with navigation placeholder
- [ ] Project builds with zero warnings
- [ ] Runs standalone and via Aspire AppHost

## Project File

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Aspire client integration -->
    <PackageReference Include="Aspire.Azure.Storage.Blobs" />
    <PackageReference Include="Aspire.Microsoft.Azure.Cosmos" />
    
    <!-- Orleans -->
    <PackageReference Include="Microsoft.Orleans.Server" />
    
    <!-- Mississippi framework -->
    <ProjectReference Include="..\..\..\src\EventSourcing.UxProjections.SignalR\EventSourcing.UxProjections.SignalR.csproj" />
    <ProjectReference Include="..\..\..\src\Core.Cosmos\Core.Cosmos.csproj" />
    <ProjectReference Include="..\..\..\src\EventSourcing.Snapshots.Cosmos\EventSourcing.Snapshots.Cosmos.csproj" />
    <ProjectReference Include="..\..\..\src\Hosting\Hosting.csproj" />
    
    <!-- Domain -->
    <ProjectReference Include="..\Cascade.Domain\Cascade.Domain.csproj" />
  </ItemGroup>
</Project>
```

## Program.cs Structure

```csharp
using Cascade.Domain;
using Mississippi.Hosting;
using Mississippi.EventSourcing.UxProjections.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry)
builder.AddServiceDefaults();

// Add Aspire-managed clients
builder.AddAzureCosmosClient("cosmos");
builder.AddAzureBlobClient("blobs");

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Mississippi event sourcing
builder.AddEventSourcing();

// Add domain
builder.Services.AddCascadeDomain();

// Add SignalR for real-time projections
builder.Services.AddSignalR();
builder.Services.AddUxProjectionSignalR();

// Configure Orleans
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()
        .Configure<ClusterOptions>(opt =>
        {
            opt.ClusterId = "cascade-dev";
            opt.ServiceId = "cascade";
        })
        .AddMemoryGrainStorage("PubSubStore")
        .AddMemoryStreams("BrookStreams");
});

// Add Cosmos storage providers (using Aspire connection)
builder.Services.AddCosmosBrookStorageProvider(options =>
{
    options.DatabaseId = "cascade-db";
    options.ContainerId = "events";
});

builder.Services.AddCosmosSnapshotStorageProvider(options =>
{
    options.DatabaseId = "cascade-db";
    options.ContainerId = "snapshots";
});

var app = builder.Build();

// Configure pipeline
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// Map SignalR hub
app.MapUxProjectionHub("/hubs/projections");

// Map Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

## Blazor Components Structure

```
Cascade.Server/
├── Components/
│   ├── App.razor              # Root component
│   ├── Routes.razor           # Router
│   ├── Layout/
│   │   ├── MainLayout.razor   # Sidebar + content layout
│   │   └── NavMenu.razor      # Navigation component
│   └── Pages/
│       ├── Home.razor         # Landing/redirect
│       ├── Login.razor        # Username entry
│       └── Error.razor        # Error page
├── wwwroot/
│   ├── css/
│   │   └── app.css
│   └── favicon.ico
├── Program.cs
└── appsettings.json
```

## TDD Steps

This task is primarily infrastructure; component tests come in Phase 4.

1. **Create**: Add project structure and Program.cs
2. **Add to Solution**: Add to `samples.sln` and `samples.slnx`
3. **Build**: Verify zero warnings
4. **Run**: Start via AppHost, verify:
   - Blazor renders
   - SignalR hub responds
   - Orleans dashboard (if enabled) shows silo

## Files to Create

- `samples/Cascade/Cascade.Server/Cascade.Server.csproj`
- `samples/Cascade/Cascade.Server/Program.cs`
- `samples/Cascade/Cascade.Server/appsettings.json`
- `samples/Cascade/Cascade.Server/appsettings.Development.json`
- `samples/Cascade/Cascade.Server/Properties/launchSettings.json`
- `samples/Cascade/Cascade.Server/Components/App.razor`
- `samples/Cascade/Cascade.Server/Components/Routes.razor`
- `samples/Cascade/Cascade.Server/Components/Layout/MainLayout.razor`
- `samples/Cascade/Cascade.Server/Components/Layout/NavMenu.razor`
- `samples/Cascade/Cascade.Server/Components/Pages/Home.razor`
- `samples/Cascade/Cascade.Server/Components/Pages/Login.razor`
- `samples/Cascade/Cascade.Server/Components/Pages/Error.razor`
- `samples/Cascade/Cascade.Server/wwwroot/css/app.css`

## Notes

- Uses Interactive Server mode for direct Orleans grain access from components
- Session state for username (no auth library)
- SignalR hub at `/hubs/projections` for real-time subscriptions
- Orleans silo is single-node dev mode; production would use clustering
