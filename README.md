# Mississippi Framework

> ⚠️ **EARLY ALPHA - WORK IN PROGRESS**: This framework is currently in early alpha development stage and not yet at version 1.0. APIs may change significantly without notice. Not recommended for production use at this time.

## What is Mississippi?

Mississippi is an **event-driven framework** for building scalable .NET applications with built-in support for event sourcing, Orleans-based distributed computing, and real-time UX synchronization.

**Core value:** Define your domain logic once (aggregates, projections, commands, reducers, events) and your UX can subscribe to it automatically—no additional plumbing required.

### Key Features

- 🎯 **Event Sourcing** - Domain events as the source of truth with Brooks event store
- 🌊 **Real-time Subscriptions** - Inlet + SignalR for automatic UX updates
- 📦 **Redux-style State** - Reservoir for predictable client-side state management
- ⚙️ **Orleans Integration** - Distributed grain-based computing
- ☁️ **Azure Native** - Cosmos DB and Azure Blob Storage support
- 🧪 **High Test Coverage** - Mutation testing with Stryker

## Documentation

📚 **[Read the full documentation →](https://gibbs-morris.github.io/mississippi/)**

Start with the [**Architecture Overview**](https://gibbs-morris.github.io/mississippi/next/architecture) to understand how data flows through the system.

## Technology Stack

- **.NET 9.0** with C# 13.0
- **Microsoft Orleans** for distributed applications
- **Azure Cosmos DB** for event storage
- **SignalR** for real-time communication

## Quick Start

### Prerequisites

- .NET 9.0 SDK or later
- PowerShell 7.0 or later

### Installation

The Mississippi Framework will be available as NuGet packages (package names TBD).

### Building from Source

```powershell
# Clone the repository
git clone https://github.com/Gibbs-Morris/mississippi.git
cd mississippi

# Run the full pipeline
pwsh ./go.ps1
```

For more build options and testing instructions, see the [documentation](https://gibbs-morris.github.io/mississippi/).

## Sample Applications

The repository includes sample applications:

- **Cascade** - Full-featured chat application demonstrating event sourcing, projections, and real-time updates
- **Crescent** - API and console applications showing basic framework usage

See the [samples directory](./samples) for more details.

## Contributing

Contributions are welcome! Please follow standard GitHub flow:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
