# AspNetCore.Orleans L1 Integration Tests

This project contains L1 integration tests for the AspNetCore.Orleans adapters. L1 tests use Orleans TestCluster infrastructure for more realistic testing scenarios.

## Test Structure

- **Infrastructure/** - TestCluster setup and shared fixtures
- **Caching/** - OrleansDistributedCache integration tests
- **OutputCaching/** - OrleansOutputCacheStore integration tests
- **Authentication/** - OrleansTicketStore integration tests
- **SignalR/** - OrleansHubLifetimeManager integration tests

## Test Characteristics

L1 tests:
- Use Orleans TestCluster for grain activation and lifecycle
- Test full integration with Orleans runtime
- Slower than L0 tests but provide higher confidence
- Include infrastructure dependencies (Orleans cluster)

## Running Tests

```bash
# Run all L1 tests
dotnet test

# Run specific test class
dotnet test --filter FullyQualifiedName~OrleansDistributedCacheTests
```

## Coverage

These tests focus on integration scenarios including:
- Full round-trip operations through Orleans grains
- Expiration and TTL behavior
- Tag-based eviction (OutputCache)
- Authentication ticket serialization
- SignalR connection lifecycle and group management
