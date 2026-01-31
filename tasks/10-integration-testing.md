# Task 10: Integration Testing (L2)

## Objective

Create end-to-end integration tests that validate the complete money transfer saga flow, including real infrastructure (Orleans silo, SignalR, projections) via Aspire AppHost.

## Rationale

L2 tests verify the entire stack works together:
- Saga starts via generated endpoint
- Steps execute against real aggregate grains
- Events propagate through streams to projections
- SignalR delivers real-time balance updates
- Compensation works when steps fail

## Test Project Setup

### 1. Create Test Project

**Location:** `tests/Spring.Domain.L2Tests/Spring.Domain.L2Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>Spring.Domain.L2Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\samples\Spring\Spring.Domain\Spring.Domain.csproj" />
    <ProjectReference Include="..\Spring.L2Tests.AppHost\Spring.L2Tests.AppHost.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

</Project>
```

### 2. Create AppHost for Tests

**Location:** `tests/Spring.L2Tests.AppHost/Spring.L2Tests.AppHost.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\samples\Spring\Spring.Silo\Spring.Silo.csproj" />
    <ProjectReference Include="..\..\samples\Spring\Spring.Api\Spring.Api.csproj" />
  </ItemGroup>

</Project>
```

**Location:** `tests/Spring.L2Tests.AppHost/Program.cs`

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos emulator for event store
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator();

var cosmosDb = cosmos.AddDatabase("springdb");

// Add Orleans silo
var silo = builder.AddProject<Projects.Spring_Silo>("silo")
    .WithReference(cosmosDb)
    .WaitFor(cosmos);

// Add API
var api = builder.AddProject<Projects.Spring_Api>("api")
    .WithReference(silo)
    .WaitFor(silo);

builder.Build().Run();
```

## Test Cases

### 1. Happy Path - Successful Transfer

**File:** `tests/Spring.Domain.L2Tests/TransferFunds/TransferFundsSagaTests.cs`

```csharp
using Aspire.Hosting.Testing;

using FluentAssertions;

using Microsoft.AspNetCore.SignalR.Client;

using Xunit;


namespace Spring.Domain.L2Tests.TransferFunds;

public sealed class TransferFundsSagaTests : IAsyncLifetime
{
    private DistributedApplication? app;
    private HttpClient? apiClient;
    private string apiBaseUrl = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Spring_L2Tests_AppHost>();
        
        app = await appHost.BuildAsync();
        await app.StartAsync();
        
        apiBaseUrl = app.GetEndpoint("api", "https").ToString();
        apiClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    }

    public async Task DisposeAsync()
    {
        apiClient?.Dispose();
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task TransferFunds_BetweenTwoAccounts_TransfersBalance()
    {
        // Arrange - Create and fund source account
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        
        await CreateAccountAsync(sourceId, "Alice");
        await DepositAsync(sourceId, 1000m);
        
        await CreateAccountAsync(destId, "Bob");
        await DepositAsync(destId, 500m);
        
        // Act - Start transfer saga
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferAsync(sagaId, sourceId, destId, 250m);
        
        // Wait for saga completion (with delay, ~15s max)
        await WaitForSagaCompletionAsync(sagaId, timeout: TimeSpan.FromSeconds(20));
        
        // Assert - Balances updated
        var sourceBalance = await GetBalanceAsync(sourceId);
        var destBalance = await GetBalanceAsync(destId);
        
        sourceBalance.Should().Be(750m);  // 1000 - 250
        destBalance.Should().Be(750m);    // 500 + 250
    }

    [Fact]
    public async Task TransferFunds_InsufficientFunds_Fails()
    {
        // Arrange
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        
        await CreateAccountAsync(sourceId, "Alice");
        await DepositAsync(sourceId, 100m);
        
        await CreateAccountAsync(destId, "Bob");
        
        // Act - Try to transfer more than available
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferAsync(sagaId, sourceId, destId, 500m);
        
        // Wait for saga to fail
        await WaitForSagaFailureAsync(sagaId, timeout: TimeSpan.FromSeconds(10));
        
        // Assert - Source balance unchanged
        var sourceBalance = await GetBalanceAsync(sourceId);
        sourceBalance.Should().Be(100m);
    }

    [Fact]
    public async Task TransferFunds_DestinationClosed_CompensatesSourceDebit()
    {
        // Arrange
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        
        await CreateAccountAsync(sourceId, "Alice");
        await DepositAsync(sourceId, 1000m);
        
        await CreateAccountAsync(destId, "Bob");
        await CloseAccountAsync(destId); // Close destination
        
        // Act - Start transfer (should fail at credit step)
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferAsync(sagaId, sourceId, destId, 250m);
        
        // Wait for saga failure and compensation
        await WaitForSagaFailureAsync(sagaId, timeout: TimeSpan.FromSeconds(20));
        
        // Assert - Source balance restored via compensation
        var sourceBalance = await GetBalanceAsync(sourceId);
        sourceBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task TransferFunds_SignalR_ReceivesBalanceUpdates()
    {
        // Arrange
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        
        await CreateAccountAsync(sourceId, "Alice");
        await DepositAsync(sourceId, 1000m);
        await CreateAccountAsync(destId, "Bob");
        
        var balanceUpdates = new List<(string AccountId, decimal Balance)>();
        
        await using var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/hubs/projections")
            .Build();
        
        hubConnection.On<object>("ProjectionUpdated", update =>
        {
            // Parse and capture balance updates
            // (exact parsing depends on projection DTO shape)
        });
        
        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("Subscribe", 
            "BankAccountBalanceProjection", 
            new[] { sourceId, destId });
        
        // Act
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferAsync(sagaId, sourceId, destId, 100m);
        
        await WaitForSagaCompletionAsync(sagaId, timeout: TimeSpan.FromSeconds(20));
        
        // Allow SignalR messages to arrive
        await Task.Delay(500);
        
        // Assert - Received updates for both accounts
        balanceUpdates.Should().Contain(u => u.AccountId == sourceId && u.Balance == 900m);
        balanceUpdates.Should().Contain(u => u.AccountId == destId && u.Balance == 100m);
    }

    [Fact]
    public async Task TransferFunds_SagaStatusProjection_TracksProgress()
    {
        // Arrange
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        
        await CreateAccountAsync(sourceId, "Alice");
        await DepositAsync(sourceId, 1000m);
        await CreateAccountAsync(destId, "Bob");
        
        var statusUpdates = new List<string>();
        
        await using var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/hubs/projections")
            .Build();
        
        string sagaId = Guid.NewGuid().ToString("N");
        
        hubConnection.On<object>("ProjectionUpdated", update =>
        {
            // Capture status progression
        });
        
        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("Subscribe", 
            "TransferFundsSagaStatusProjection", 
            new[] { sagaId });
        
        // Act
        await StartTransferAsync(sagaId, sourceId, destId, 100m);
        await WaitForSagaCompletionAsync(sagaId, timeout: TimeSpan.FromSeconds(20));
        
        // Assert - Status progressed through expected states
        statusUpdates.Should().ContainInOrder(
            "Initiated",
            "SourceDebited",
            "DestinationCredited",
            "Completed");
    }

    // Helper methods

    private async Task CreateAccountAsync(string accountId, string holderName)
    {
        var response = await apiClient!.PostAsJsonAsync(
            $"/api/aggregates/bank-accounts/{accountId}/open-account",
            new { HolderName = holderName });
        response.EnsureSuccessStatusCode();
    }

    private async Task DepositAsync(string accountId, decimal amount)
    {
        var response = await apiClient!.PostAsJsonAsync(
            $"/api/aggregates/bank-accounts/{accountId}/deposit-funds",
            new { Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    private async Task CloseAccountAsync(string accountId)
    {
        var response = await apiClient!.PostAsJsonAsync(
            $"/api/aggregates/bank-accounts/{accountId}/close-account",
            new { });
        response.EnsureSuccessStatusCode();
    }

    private async Task StartTransferAsync(
        string sagaId, 
        string sourceAccountId, 
        string destinationAccountId, 
        decimal amount)
    {
        var response = await apiClient!.PostAsJsonAsync(
            $"/api/sagas/transfer-funds/{sagaId}",
            new 
            { 
                SourceAccountId = sourceAccountId,
                DestinationAccountId = destinationAccountId,
                Amount = amount
            });
        response.EnsureSuccessStatusCode();
    }

    private async Task<decimal> GetBalanceAsync(string accountId)
    {
        var response = await apiClient!.GetFromJsonAsync<dynamic>(
            $"/api/projections/bank-account-balance/{accountId}");
        return (decimal)response!.balance;
    }

    private async Task WaitForSagaCompletionAsync(string sagaId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var status = await GetSagaStatusAsync(sagaId);
            if (status == "Completed")
                return;
            await Task.Delay(500);
        }
        throw new TimeoutException($"Saga {sagaId} did not complete within {timeout}");
    }

    private async Task WaitForSagaFailureAsync(string sagaId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var status = await GetSagaStatusAsync(sagaId);
            if (status == "Failed" || status == "Compensated")
                return;
            await Task.Delay(500);
        }
        throw new TimeoutException($"Saga {sagaId} did not fail within {timeout}");
    }

    private async Task<string> GetSagaStatusAsync(string sagaId)
    {
        try
        {
            var response = await apiClient!.GetFromJsonAsync<dynamic>(
                $"/api/projections/transfer-funds-saga-status/{sagaId}");
            return (string)response!.status;
        }
        catch
        {
            return "Unknown";
        }
    }
}
```

### 2. Concurrent Transfer Tests

```csharp
[Fact]
public async Task TransferFunds_ConcurrentTransfers_AllSucceed()
{
    // Arrange - Create accounts with sufficient funds
    string sharedSourceId = $"source-{Guid.NewGuid():N}";
    await CreateAccountAsync(sharedSourceId, "Shared Source");
    await DepositAsync(sharedSourceId, 10_000m);
    
    var destinations = new List<string>();
    for (int i = 0; i < 5; i++)
    {
        string destId = $"dest-{i}-{Guid.NewGuid():N}";
        await CreateAccountAsync(destId, $"Destination {i}");
        destinations.Add(destId);
    }
    
    // Act - Start 5 concurrent transfers
    var tasks = destinations.Select(async (destId, i) =>
    {
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferAsync(sagaId, sharedSourceId, destId, 100m);
        return sagaId;
    }).ToList();
    
    var sagaIds = await Task.WhenAll(tasks);
    
    // Wait for all to complete
    await Task.WhenAll(sagaIds.Select(id => 
        WaitForSagaCompletionAsync(id, TimeSpan.FromSeconds(30))));
    
    // Assert
    var sourceBalance = await GetBalanceAsync(sharedSourceId);
    sourceBalance.Should().Be(9_500m); // 10000 - (5 * 100)
    
    foreach (var destId in destinations)
    {
        var destBalance = await GetBalanceAsync(destId);
        destBalance.Should().Be(100m);
    }
}
```

### 3. Delay Verification Test

```csharp
[Fact]
public async Task TransferFunds_StepDelay_TakesAtLeast10Seconds()
{
    // Arrange
    string sourceId = $"source-{Guid.NewGuid():N}";
    string destId = $"dest-{Guid.NewGuid():N}";
    
    await CreateAccountAsync(sourceId, "Alice");
    await DepositAsync(sourceId, 1000m);
    await CreateAccountAsync(destId, "Bob");
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    
    string sagaId = Guid.NewGuid().ToString("N");
    await StartTransferAsync(sagaId, sourceId, destId, 100m);
    await WaitForSagaCompletionAsync(sagaId, timeout: TimeSpan.FromSeconds(20));
    
    stopwatch.Stop();
    
    // Assert - Should take at least 10 seconds due to delay
    stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(9));
}
```

## File Summary

| File | Purpose |
|------|---------|
| `tests/Spring.Domain.L2Tests/Spring.Domain.L2Tests.csproj` | Test project |
| `tests/Spring.L2Tests.AppHost/Spring.L2Tests.AppHost.csproj` | Aspire app host |
| `tests/Spring.L2Tests.AppHost/Program.cs` | Test infrastructure setup |
| `tests/Spring.Domain.L2Tests/TransferFunds/TransferFundsSagaTests.cs` | Integration tests |

## Acceptance Criteria

- [ ] L2 test project created with Aspire hosting
- [ ] AppHost provisions Cosmos emulator and silo
- [ ] Tests start via `dotnet test --filter "FullyQualifiedName~L2Tests"`
- [ ] Happy path transfer test passes
- [ ] Insufficient funds test passes
- [ ] Compensation test passes (closed destination)
- [ ] SignalR balance update test passes
- [ ] Saga status progression test passes
- [ ] Concurrent transfers test passes
- [ ] Delay verification test passes (>10s execution)

## Run Instructions

```powershell
# Run L2 tests only
dotnet test tests/Spring.Domain.L2Tests/Spring.Domain.L2Tests.csproj -c Release

# Or via script
pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1
```

## Notes

- L2 tests are NOT in PR gates (they take minutes to run)
- Cosmos emulator requires Docker
- Tests use real Orleans clustering (in-memory for tests)
- Timeout values account for 10-second step delay

## Dependencies

- All previous tasks completed
- Working saga infrastructure
- Working generators

## Blocked By

- [05-domain-saga](05-domain-saga.md)
- [09-delay-effect](09-delay-effect.md)

## Blocks

- Nothing (final task)
