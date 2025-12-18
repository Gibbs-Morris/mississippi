# C2: Container Diagram

This diagram shows the major containers (deployable units) within the Mississippi Framework and how they interact.

```mermaid
C4Container
    title Container diagram for Mississippi Framework

    Person(developer, "Application Developer", "Builds applications using Mississippi")

    Container_Boundary(framework, "Mississippi Framework") {
        Container(core, "Core", "C# / .NET 9.0", "Core abstractions and utilities including mapping, logging, and common patterns")
        Container(hosting, "Hosting", "C# / .NET 9.0", "Application hosting infrastructure and dependency injection setup")
        Container(aspnet, "AspNetCore.Orleans", "C# / ASP.NET Core", "Orleans integration for web applications")
        
        Container_Boundary(eventsourcing, "Event Sourcing") {
            Container(brooks, "Brooks", "C# / Orleans", "Event stream storage and retrieval using brook pattern")
            Container(aggregates, "Aggregates", "C# / Orleans", "DDD aggregate management with event sourcing")
            Container(projections, "Projections", "C# / Orleans", "Read model projections from event streams")
            Container(snapshots, "Snapshots", "C# / Orleans", "Aggregate state snapshot management")
            Container(reducers, "Reducers", "C# / .NET", "State reduction from event sequences")
            Container(effects, "Effects", "C# / .NET", "Side effect handling and execution")
            Container(serialization, "Serialization", "C# / .NET", "Event and state serialization (JSON)")
            Container(uxprojections, "UX Projections", "C# / Orleans", "User experience optimized projections")
        }
        
        Container_Boundary(storage, "Storage Providers") {
            Container(brooksCosmos, "Brooks.Cosmos", "C# / Cosmos SDK", "Cosmos DB storage for event streams")
            Container(snapshotsCosmos, "Snapshots.Cosmos", "C# / Cosmos SDK", "Cosmos DB storage for snapshots")
            Container(coreCosmos, "Core.Cosmos", "C# / Cosmos SDK", "Common Cosmos DB utilities")
        }
    }

    Container_Boundary(samples, "Sample Applications") {
        Container(apiApp, "Crescent API App", "C# / ASP.NET Core", "Sample REST API application")
        Container(consoleApp, "Crescent Console App", "C# / .NET", "Sample console application")
        Container(webApp, "Crescent Web App", "C# / Blazor", "Sample web application")
    }

    System_Ext(orleans, "Microsoft Orleans", "Distributed actor framework")
    System_Ext(cosmos, "Azure Cosmos DB", "NoSQL database")

    Rel(developer, core, "Uses")
    Rel(developer, hosting, "Uses")
    Rel(developer, aspnet, "Uses")
    Rel(developer, eventsourcing, "Uses")
    
    Rel(apiApp, hosting, "Uses")
    Rel(apiApp, aspnet, "Uses")
    Rel(apiApp, aggregates, "Uses")
    Rel(consoleApp, hosting, "Uses")
    Rel(consoleApp, aggregates, "Uses")
    Rel(webApp, aspnet, "Uses")
    Rel(webApp, uxprojections, "Uses")

    Rel(hosting, core, "Uses")
    Rel(aspnet, orleans, "Integrates with")
    
    Rel(aggregates, brooks, "Uses")
    Rel(aggregates, reducers, "Uses")
    Rel(aggregates, snapshots, "Uses")
    Rel(aggregates, serialization, "Uses")
    
    Rel(projections, brooks, "Reads from")
    Rel(projections, effects, "Uses")
    
    Rel(uxprojections, brooks, "Reads from")
    Rel(uxprojections, snapshots, "Uses")
    
    Rel(brooks, serialization, "Uses")
    Rel(brooks, brooksCosmos, "Persists via")
    Rel(snapshots, snapshotsCosmos, "Persists via")
    
    Rel(brooksCosmos, coreCosmos, "Uses")
    Rel(snapshotsCosmos, coreCosmos, "Uses")
    Rel(brooksCosmos, cosmos, "Writes to")
    Rel(snapshotsCosmos, cosmos, "Writes to")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="2")
```

## Key Containers

### Core Infrastructure
- **Core**: Foundation abstractions and utilities
- **Hosting**: Application startup and DI configuration
- **AspNetCore.Orleans**: Web application integration

### Event Sourcing Components
- **Brooks**: Event stream storage with append-only logs
- **Aggregates**: Domain aggregates with command handling
- **Projections**: Read model generation from events
- **Snapshots**: Performance optimization via state snapshots
- **Reducers**: Event sequence to state transformation
- **Effects**: Async side effect coordination
- **Serialization**: JSON-based event serialization
- **UX Projections**: UI-optimized read models

### Storage Providers
- **Brooks.Cosmos**: Cosmos DB event stream storage
- **Snapshots.Cosmos**: Cosmos DB snapshot storage
- **Core.Cosmos**: Shared Cosmos DB utilities

### Sample Applications
- **Crescent API App**: REST API example
- **Crescent Console App**: CLI example
- **Crescent Web App**: Blazor web UI example
