# Phase 5: E2E Tests (L2) – Playwright via Aspire

**Status**: ✅ Complete

## Goal

Create Playwright-based end-to-end tests that validate the complete chat application flow across multiple users, testing real-time functionality. **Tests execute via Aspire orchestration** using `Aspire.Hosting.Testing`.

## Test Execution

Tests use `Aspire.Hosting.Testing` to start the full infrastructure:

```csharp
// In PlaywrightFixture
var appHost = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.Cascade_AppHost>();

app = await appHost.BuildAsync();
await app.StartAsync();  // Starts Cosmos Emulator, Azurite, Cascade.Server
```

This ensures:

- Full Orleans silo with Cosmos storage
- Real SignalR connections
- Containerized dependencies (Azurite, Cosmos Emulator)
- Automatic cleanup on test completion

## Test Project Structure

```text
samples/Cascade/
└── Cascade.L2Tests/
    ├── Cascade.L2Tests.csproj
    ├── GlobalUsings.cs
    ├── PlaywrightFixture.cs           # Aspire app startup fixture
    ├── TestBase.cs                    # Base class with common helpers
    ├── Features/
    │   ├── LoginTests.cs              # User login flow
    │   ├── ChannelCreationTests.cs    # Create and join channels
    │   ├── MessagingTests.cs          # Send/receive messages
    │   └── RealTimeTests.cs           # Multi-user real-time sync
    └── PageObjects/
        ├── LoginPage.cs
        ├── ChannelListPage.cs
        └── ChannelViewPage.cs
```

## Tasks

| Task | File | Status |
| ------ | ------ | -------- |
| 5.1 Playwright L2 Tests via Aspire | [01-playwright-tests.md](./01-playwright-tests.md) | ✅ |

## Acceptance Criteria

- [x] Playwright test project created with proper L2 naming
- [x] **Test fixture starts Aspire AppHost** (full infrastructure)
- [x] Tests run against real Orleans/Cosmos/SignalR stack
- [x] Multi-browser tests for real-time scenarios
- [x] Page object pattern for maintainability
- [x] Tests pass in CI environment (Docker/Podman required)
- [x] Proper cleanup via `DisposeAsync` on fixture

## Test Scenarios

### Login Flow

1. Navigate to app → redirects to login
2. Enter display name → redirects to channels
3. User sees empty channel list

### Channel Creation

1. User creates channel "general"
2. Channel appears in channel list
3. User can select channel

### Messaging

1. User A sends message in channel
2. Message appears in User A's view
3. User B (in separate browser) sees message appear in real-time

### Real-Time Sync

1. User A and User B both viewing "general" channel
2. User A sends message
3. User B sees message without refresh (SignalR)
4. Latency under 2 seconds

## Key Technologies

| Package | Purpose |
| --------- | --------- |
| `Microsoft.Playwright` | Browser automation |
| `Microsoft.Playwright.NUnit` or `xunit` | Test framework integration |
| `Aspire.Hosting.Testing` | Start AppHost in tests |

## Test Levels Reference

Per repository conventions:

- **L0**: Unit tests (mocks, no IO)
- **L1**: Light integration (temp files, in-proc)
- **L2**: Functional tests with real dependencies
- **L3**: Full E2E with production-like environment
- **L4**: Synthetic monitoring

This project is **L2** because it:

- Uses real Aspire-managed containers (Cosmos, Azurite)
- Tests real Orleans grains and SignalR connections
- Validates functional behavior across the full stack
