---
sidebar_position: 1
title: Getting Started
description: Install Mississippi and build your first event-sourced application
---

This section guides you through installing Mississippi and building your first
application. By the end, you'll have a working event-sourced system with
real-time updates.

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Azure Cosmos DB account (or emulator for local development)
- Azure Storage account (or Azurite for local development)

### SDK Packages

Install the appropriate SDK package for your project type:

```bash
# Blazor WebAssembly client
dotnet add package Mississippi.Sdk.Client

# ASP.NET API server
dotnet add package Mississippi.Sdk.Server

# Orleans silo host
dotnet add package Mississippi.Sdk.Silo
```

See the [SDK Reference](../platform/sdk.md) for package contents.

## Quick Start by Project Type

### Client-Side State Management

If you're building a Blazor WebAssembly app and want Redux-style state
management:

→ [Reservoir Getting Started](../platform/reservoir/getting-started.md)

This tutorial builds a shopping cart with actions, reducers, and effects.

### Full Event-Sourced System

If you're building a complete event-sourced backend with real-time updates:

→ [Spring Sample Application](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring)

The Spring sample demonstrates:

- Aggregate definition and command handling
- Event storage with Brooks
- UX Projections for read models
- Real-time updates with Aqueduct and Inlet
- Blazor WebAssembly client with Reservoir

## Project Structure

A typical Mississippi solution has three projects:

```text
MyApp/
├── MyApp.Client/           # Blazor WebAssembly
│   ├── Features/          # Reservoir state slices
│   └── Pages/             # Blazor components
├── MyApp.Server/           # ASP.NET API
│   ├── Controllers/       # Command and query endpoints
│   └── Hubs/              # SignalR hubs
└── MyApp.Silo/             # Orleans host
    ├── Aggregates/        # Command handlers
    └── Projections/       # Read models
```

## Configuration

### Silo Configuration

```csharp
// Program.cs in Silo project
builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();

    // Add Mississippi components
    silo.AddMississippi(options =>
    {
        options.UseCosmosBrooks(cosmos =>
        {
            cosmos.ConnectionString = "...";
            cosmos.DatabaseName = "events";
        });

        options.UseAzureBlobSnapshots(blob =>
        {
            blob.ConnectionString = "...";
            blob.ContainerName = "snapshots";
        });
    });
});
```

### Server Configuration

```csharp
// Program.cs in Server project
builder.Services.AddMississippiServer();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapMississippiEndpoints();
app.MapHub<InletHub>("/inlet");
```

### Client Configuration

```csharp
// Program.cs in Client project
builder.Services.AddReservoir();
builder.Services.AddInletClient(options =>
{
    options.HubUrl = "https://api.example.com/inlet";
});
```

## Next Steps

- [Concepts](../concepts/index.md) — Understand the architecture
- [Components](../platform/index.md) — Learn each component in depth
- [Reservoir Tutorial](../platform/reservoir/getting-started.md) — Build a client app

## Related Topics

- [SDK Reference](../platform/sdk.md) — Package contents and versions
- [Architecture](../concepts/architecture.md) — System deployment model
