# C1: System Context Diagram

This diagram shows the Mississippi Framework and how it interacts with external systems and users.

```mermaid
graph TB
    subgraph Users
        Developer["👤 Application Developer<br/>Builds distributed applications<br/>using Mississippi Framework"]
        EndUser["👤 End User<br/>Uses applications built<br/>with Mississippi Framework"]
    end
    
    Mississippi["<b>Mississippi Framework</b><br/>[.NET System]<br/><br/>Event sourcing and distributed<br/>computing framework for .NET<br/>applications using Orleans"]
    
    subgraph "External Systems"
        Orleans["<b>Microsoft Orleans</b><br/>[Distributed Framework]<br/><br/>Distributed virtual actor framework"]
        Cosmos["<b>Azure Cosmos DB</b><br/>[Database]<br/><br/>Cloud-native NoSQL database<br/>for event and snapshot storage"]
        DotNet["<b>.NET 9.0 Runtime</b><br/>[Platform]<br/><br/>Application runtime platform"]
    end
    
    Developer -->|Uses via<br/>NuGet packages| Mississippi
    EndUser -->|Interacts with<br/>applications built on| Mississippi
    Mississippi -->|Built on<br/>Orleans SDK| Orleans
    Mississippi -->|Stores data in<br/>Cosmos DB SDK| Cosmos
    Mississippi -->|Runs on| DotNet
    
    classDef person fill:#08427B,stroke:#052E56,color:#fff
    classDef system fill:#1168BD,stroke:#0B4884,color:#fff
    classDef external fill:#999,stroke:#666,color:#fff
    
    class Developer,EndUser person
    class Mississippi system
    class Orleans,Cosmos,DotNet external
```

## Key Elements

- **Mississippi Framework**: The core system providing event sourcing, CQRS, and distributed computing capabilities
- **Application Developer**: Uses the framework to build scalable distributed applications
- **End User**: Consumes applications built with Mississippi
- **Microsoft Orleans**: Foundation for distributed computing and virtual actors
- **Azure Cosmos DB**: Persistent storage for events and snapshots
- **.NET 9.0 Runtime**: Execution platform
