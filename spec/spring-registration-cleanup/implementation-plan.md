# Implementation Plan

## Step-by-Step Checklist

### Phase 1: Spring.Silo Refactoring

- [ ] **1.1** Create `Spring.Silo/Infrastructure/` folder
- [ ] **1.2** Create `SpringObservabilityRegistrations.cs` with `AddSpringObservability(this WebApplicationBuilder)`
- [ ] **1.3** Create `SpringAspireRegistrations.cs` with `AddSpringAspireResources(this WebApplicationBuilder)`
- [ ] **1.4** Create `SpringDomainRegistrations.cs` with `AddSpringDomain(this IServiceCollection)`
- [ ] **1.5** Create `SpringEventSourcingRegistrations.cs` with `AddSpringEventSourcing(this IServiceCollection)`
- [ ] **1.6** Create `SpringOrleansRegistrations.cs` with `AddSpringOrleansSilo(this WebApplicationBuilder)`
- [ ] **1.7** Create `SpringHealthRegistrations.cs` with `MapSpringHealthCheck(this WebApplication)`
- [ ] **1.8** Refactor `Program.cs` to use new extension methods
- [ ] **1.9** Build and verify zero warnings

### Phase 2: Spring.Server Refactoring

- [ ] **2.1** Create `Spring.Server/Infrastructure/` folder
- [ ] **2.2** Create `SpringServerObservabilityRegistrations.cs` with `AddSpringServerObservability(this WebApplicationBuilder)`
- [ ] **2.3** Create `SpringServerOrleansRegistrations.cs` with `AddSpringOrleansClient(this WebApplicationBuilder)`
- [ ] **2.4** Create `SpringServerApiRegistrations.cs` with `AddSpringApi(this IServiceCollection)`
- [ ] **2.5** Create `SpringServerRealtimeRegistrations.cs` with `AddSpringRealtime(this IServiceCollection)`
- [ ] **2.6** Create `SpringServerMiddlewareRegistrations.cs` with `UseSpringMiddleware(this WebApplication)` and `MapSpringEndpoints(this WebApplication)`
- [ ] **2.7** Refactor `Program.cs` to use new extension methods
- [ ] **2.8** Build and verify zero warnings

### Phase 3: Validation

- [ ] **3.1** Run `./eng/src/agent-scripts/build-sample-solution.ps1`
- [ ] **3.2** Run `./eng/src/agent-scripts/clean-up-sample-solution.ps1`
- [ ] **3.3** Run `./eng/src/agent-scripts/unit-test-sample-solution.ps1`
- [ ] **3.4** Commit changes

## Files to Create

| File | Purpose |
|------|---------|
| `Spring.Silo/Infrastructure/SpringObservabilityRegistrations.cs` | OpenTelemetry tracing, metrics, logging |
| `Spring.Silo/Infrastructure/SpringAspireRegistrations.cs` | Azure Table/Blob/Cosmos clients with keyed forwarding |
| `Spring.Silo/Infrastructure/SpringDomainRegistrations.cs` | Generated aggregate + projection registrations + services |
| `Spring.Silo/Infrastructure/SpringEventSourcingRegistrations.cs` | JSON, Brooks, Snapshots, Cosmos providers |
| `Spring.Silo/Infrastructure/SpringOrleansRegistrations.cs` | UseOrleans with Aqueduct + EventSourcing |
| `Spring.Silo/Infrastructure/SpringHealthRegistrations.cs` | Health check endpoint |
| `Spring.Server/Infrastructure/SpringServerObservabilityRegistrations.cs` | OpenTelemetry (lighter) |
| `Spring.Server/Infrastructure/SpringServerOrleansRegistrations.cs` | Orleans client |
| `Spring.Server/Infrastructure/SpringServerApiRegistrations.cs` | Controllers + OpenAPI + Scalar |
| `Spring.Server/Infrastructure/SpringServerRealtimeRegistrations.cs` | SignalR + Aqueduct + Inlet + mappers |
| `Spring.Server/Infrastructure/SpringServerMiddlewareRegistrations.cs` | Middleware + endpoint mapping |

## Files to Modify

| File | Changes |
|------|---------|
| `Spring.Silo/Program.cs` | Replace inline registrations with extension method calls |
| `Spring.Server/Program.cs` | Replace inline registrations with extension method calls |

## Test Plan

1. Build both projects with zero warnings
2. Run existing Spring.Domain.L0Tests
3. Run existing Spring.L2Tests (if L2 tests exist and pass currently)
4. Manual smoke test: verify silo and server start without errors

## Rollout Plan

This is sample code, no staged rollout needed. Commit all changes together.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Missing using directive | Build fails fast; easy to fix |
| Wrong registration order | Keep Inlet ordering (AddInletSilo before ScanProjectionAssemblies) |
| Generated code namespace collision | Use `Infrastructure` namespace, not `Registrations` |
