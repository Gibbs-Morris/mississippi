# Mississippi Framework

> ⚠️ **EARLY ALPHA - WORK IN PROGRESS**: This framework is currently in early alpha development stage and not yet at version 1.0. APIs may change significantly without notice. Not recommended for production use at this time.

## Overview
Mississippi is a sophisticated .NET framework designed to streamline distributed application development. It provides a robust foundation for building scalable, maintainable .NET applications with built-in support for mapping, distributed computing, cloud storage integration, and workflow management.

## Technology Stack
- **.NET 9.0** - Latest .NET runtime with C# 13.0
- **Microsoft Orleans** - Framework for building distributed applications
- **Azure Cosmos DB** - Cloud-native NoSQL database integration

## Development Tooling
- **xUnit** - Unit testing framework
- **Stryker** - Mutation testing for validating test quality
- **SonarAnalyzer** - Static code analysis with SonarCloud gates
- **StyleCop** - Code style enforcement
- **GitVersion** - Semantic versioning

## Getting Started
### Prerequisites
- .NET 9.0 SDK or later
- PowerShell 7.0 or later (for build scripts)
- JetBrains Rider or other compatible IDE

### Installation
The Mississippi Framework will be available as a set of NuGet packages. Package names to be confirmed.

### Building from Source
To build the project from source:

```powershell
# Clone the repository
git clone https://github.com/Gibbs-Morris/mississippi.git
cd mississippi

# Run the build script
./build.ps1
```

Build script options:
- `-SkipBuild` - Skip the build step, useful when you just want to run tests
- `-SkipTests` - Skip running tests, useful when you just want to build the code
- `-SkipStryker` - Skip Stryker mutation testing (which can be time-consuming)
- `-Configuration <Debug|Release>` - Build configuration (default is Release)

## Samples
The repository includes sample applications demonstrating the framework:

- **CrescentApiApp** - An ASP.NET Core API application showing web service integration
- **CrescentConsoleApp** - A console application demonstrating non-web usage

## Testing
The framework includes comprehensive testing:

```powershell
# Run tests with code coverage
./build.ps1 -SkipStryker

# Run tests including mutation testing (Stryker)
./build.ps1
```

Test results and coverage reports are generated in the `test-results` directory.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.

## Contributing
Contributions to the Mississippi Framework are welcome. Please follow standard GitHub flow:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request