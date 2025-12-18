# C1: System Context Diagram

This diagram shows the Mississippi Framework and how it interacts with external systems and users.

```mermaid
C4Context
    title System Context diagram for Mississippi Framework

    Person(developer, "Application Developer", "Builds distributed applications using Mississippi Framework")
    Person(endUser, "End User", "Uses applications built with Mississippi Framework")
    
    System(mississippi, "Mississippi Framework", "Event sourcing and distributed computing framework for .NET applications using Orleans and event-driven architecture")
    
    System_Ext(orleans, "Microsoft Orleans", "Distributed virtual actor framework")
    System_Ext(cosmos, "Azure Cosmos DB", "Cloud-native NoSQL database for event and snapshot storage")
    System_Ext(dotnet, ".NET 9.0 Runtime", "Application runtime platform")
    
    Rel(developer, mississippi, "Uses", "NuGet packages")
    Rel(mississippi, orleans, "Built on", "Orleans SDK")
    Rel(mississippi, cosmos, "Stores data in", "Cosmos DB SDK")
    Rel(mississippi, dotnet, "Runs on", ".NET 9.0")
    Rel(endUser, mississippi, "Interacts with applications built on")
    
    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

## Key Elements

- **Mississippi Framework**: The core system providing event sourcing, CQRS, and distributed computing capabilities
- **Application Developer**: Uses the framework to build scalable distributed applications
- **End User**: Consumes applications built with Mississippi
- **Microsoft Orleans**: Foundation for distributed computing and virtual actors
- **Azure Cosmos DB**: Persistent storage for events and snapshots
- **.NET 9.0 Runtime**: Execution platform
