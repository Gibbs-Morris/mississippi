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
- **ReSharper** - Code cleanup and formatting

## Getting Started
### Prerequisites
- .NET 9.0 SDK or later
- PowerShell 7.0 or later (for build scripts)
- JetBrains Rider or other compatible IDE

### Installation
The Mississippi Framework will be available as a set of NuGet packages. Package names to be confirmed.

### Local Development Scripts
The project includes four specialized PowerShell scripts for local development workflows:

#### 1. Quick Build Verification
```powershell
# Fast build verification without tests
./dev-build.ps1
```
Use this for quick syntax checking and build validation during active development.

#### 2. Standard Development Workflow
```powershell
# Build + tests + coverage (recommended for most development)
./dev-test.ps1
```
This is the standard script for most development scenarios. It builds the solution, runs all tests, and generates coverage reports.

#### 3. Comprehensive Quality Assurance
```powershell
# Build + tests + coverage + mutation testing
./dev-quality.ps1
```
Use this for thorough quality validation before major commits or releases. Includes Stryker mutation testing.

#### 4. Code Cleanup
```powershell
# Clean up code formatting and style issues
./dev-cleanup.ps1
```
Applies ReSharper cleanup rules to maintain consistent code quality and formatting.

### Script Usage Examples
```powershell
# Build in Debug configuration
./dev-build.ps1 -Configuration Debug

# Run tests with custom cleanup profile
./dev-cleanup.ps1 -Profile "Built-in: Reformat Code"

# Use custom solution path
./dev-test.ps1 -SolutionPath "path\to\custom.sln"
```

### Development Workflow
1. **During Development**: Use `dev-build.ps1` for quick feedback
2. **Before Committing**: Use `dev-test.ps1` to ensure quality
3. **Before Major Changes**: Use `dev-quality.ps1` for thorough validation
4. **After Merging**: Use `dev-cleanup.ps1` to fix formatting issues

## Samples
The repository includes sample applications demonstrating the framework:

- **CrescentApiApp** - An ASP.NET Core API application showing web service integration
- **CrescentConsoleApp** - A console application demonstrating non-web usage

## Testing
The framework includes comprehensive testing with multiple levels:

```powershell
# Quick build verification
./dev-build.ps1

# Standard testing with coverage
./dev-test.ps1

# Comprehensive testing with mutation testing
./dev-quality.ps1
```

Test results and coverage reports are generated in the `test-results` directory.

## CI/CD
This project uses GitHub Actions for continuous integration and deployment. The local development scripts are designed to work alongside the CI/CD pipeline, ensuring consistent quality between local development and automated builds.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.

## Contributing
Contributions to the Mississippi Framework are welcome. Please follow standard GitHub flow:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run appropriate development scripts to ensure quality
5. Submit a pull request