# Cascade L2 End-to-End Tests

This project contains Playwright-based E2E tests for the Cascade Chat sample application.

## Prerequisites

### Required

- .NET 9.0 SDK
- **Docker Desktop** (required for Aspire emulators)
- Playwright browsers installed

### Installing Playwright Browsers

After building the project, install Playwright browsers:

```powershell
# Option 1: Using the .NET tool
dotnet tool restore
playwright install chromium

# Option 2: Using the Playwright PowerShell script
pwsh bin/Release/net9.0/playwright.ps1 install chromium
```

### Docker Requirements

The Cascade sample uses .NET Aspire with the following emulated services:

- **Azure Cosmos DB Emulator** - for event sourcing storage
- **Azure Storage Emulator (Azurite)** - for blob storage and table clustering

These run as Docker containers when you execute the tests. Ensure Docker Desktop is running before executing tests.

## Running the Tests

### Build the Test Project

```powershell
dotnet build samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release
```

### Run All L2 Tests

```powershell
dotnet test samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release
```

### Run Specific Test Categories

```powershell
# Login tests only
dotnet test samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release --filter "FullyQualifiedName~LoginTests"

# Navigation tests only
dotnet test samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release --filter "FullyQualifiedName~NavigationTests"

# Channel tests only
dotnet test samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release --filter "FullyQualifiedName~Channel"

# Messaging tests only
dotnet test samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release --filter "FullyQualifiedName~MessagingTests"
```

### Run with Verbose Output

```powershell
dotnet test samples\Cascade\Cascade.L2Tests\Cascade.L2Tests.csproj -c Release -l "console;verbosity=detailed"
```

## Test Categories

| Test Class | Description | Test Count |
|------------|-------------|------------|
| `LoginTests` | Login flow and authentication | 3 |
| `ChannelCreationTests` | Creating channels and channel list | 3 |
| `MessagingTests` | Sending and viewing messages | 2 |
| `RealTimeTests` | Real-time message delivery | 2 |
| `NavigationTests` | Routing and navigation behavior | 6 |
| `UiStateTests` | UI elements and visual feedback | 9 |
| `ValidationTests` | Input validation and form behavior | 6 |
| `MultiChannelTests` | Multiple channels and switching | 5 |
| `KeyboardTests` | Keyboard accessibility | 4 |

## Test Architecture

### Fixture-Based Approach

All tests inherit from `TestBase` which uses `PlaywrightFixture`:

- **PlaywrightFixture**: Starts the Aspire AppHost with Cosmos and Storage emulators, then launches Playwright browser
- **TestBase**: Provides helper methods like `CreatePageAndLoginAsync()`

### Page Objects

- `LoginPage` - Login form interactions
- `ChannelListPage` - Channel sidebar interactions
- `ChannelViewPage` - Channel message view interactions

## Timeouts

The Aspire infrastructure startup can take 60-180 seconds on first run due to:

- Docker image pulls for Cosmos and Azurite emulators
- Cosmos DB emulator initialization
- Orleans silo startup

Subsequent runs are faster once images are cached.

## Troubleshooting

### "Container runtime 'docker' could not be found"

Docker Desktop is not running or not installed. Start Docker Desktop and try again.

### Timeout waiting for cosmos resource

The Cosmos DB emulator takes 30-60 seconds to become healthy. The fixture waits up to 180 seconds by default.

### Browser not installed

Run `playwright install chromium` to install the Chromium browser.

### Tests pass locally but fail in CI

Ensure CI has Docker available. GitHub Actions runners typically have Docker pre-installed.

## Allure Reporting

Tests are instrumented with Allure attributes for reporting:

- `[AllureParentSuite]` - Top-level grouping
- `[AllureSuite]` - Feature grouping
- `[AllureSubSuite]` - Scenario grouping

Generate Allure reports after running tests:

```powershell
allure serve allure-results
```
