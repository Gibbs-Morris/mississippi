# Phase 3: Infrastructure – Aspire & Blazor Host

**Status**: ✅ Complete

## Goal

Set up .NET Aspire orchestration for local development (Azurite + Cosmos Emulator) and create a Blazor Server host cohosted with Orleans.

## Project Structure

```
samples/Cascade/
├── Cascade.AppHost/           # Aspire orchestrator
├── Cascade.ServiceDefaults/   # Shared configuration (optional)
└── Cascade.Server/            # Blazor Server + Orleans silo
```

## Tasks

| Task | File | Status |
|------|------|--------|
| 3.1 Add Aspire Packages | [01-aspire-packages.md](./01-aspire-packages.md) | ⬜ |
| 3.2 Create AppHost | [02-apphost.md](./02-apphost.md) | ⬜ |
| 3.3 Create Blazor Server | [03-blazor-server.md](./03-blazor-server.md) | ⬜ |

## Acceptance Criteria

- [ ] Aspire 13.1.0 packages added to `Directory.Packages.props`
- [ ] `Cascade.AppHost` orchestrates Azurite and Cosmos Emulator
- [ ] `Cascade.Server` hosts Blazor Server + Orleans silo + SignalR hub
- [ ] Connection strings flow from Aspire to services
- [ ] Dashboard accessible at default Aspire URL
- [ ] Projects added to `samples.sln` and `samples.slnx`
- [ ] All projects build with zero warnings

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Cascade.AppHost                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │   Azurite   │  │ Cosmos DB   │  │   Cascade.Server    │ │
│  │  Container  │  │  Emulator   │  │ (Blazor+Orleans+SR) │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
          │                │                    │
          └────────────────┴────────────────────┘
                    Connection Strings
```

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Aspire.Hosting.AppHost` | 13.1.0 | AppHost orchestration |
| `Aspire.Hosting.Azure.CosmosDB` | 13.1.0 | Cosmos Emulator resource |
| `Aspire.Hosting.Azure.Storage` | 13.1.0 | Azurite resource |
| `Aspire.Azure.Storage.Blobs` | 13.1.0 | Client integration |
| `Aspire.Microsoft.Azure.Cosmos` | 13.1.0 | Client integration |

## Storage Pattern

Following Crescent sample patterns:
- **Cosmos DB Emulator**: Event store (brooks) and snapshots
- **Azurite Blob Storage**: Distributed locking (Orleans grain directory)

## Key Design Notes

1. **Cohosted Architecture**: Single process runs Blazor Server, Orleans silo, and SignalR hub
2. **Aspire Resources**: Containers managed by Aspire; connection strings injected automatically
3. **No Auth**: Username-only login for demo purposes (stored in session/cookie)
4. **Interactive Server**: Using Blazor Server mode (not WASM) for direct Orleans grain access
