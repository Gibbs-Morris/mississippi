# 01 Repository Findings

## Finding 1: Cosmos snapshot storage is the implementation and testing parity baseline
- **Claim**: The Blob provider should mirror the existing Cosmos snapshot provider's public shape, diagnostics wrapper, and registration style.
- **Evidence A**: `src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs:16-128` implements a thin `ISnapshotStorageProvider` facade with logging and metrics wrapped around repository operations.
- **Evidence B**: `src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs:27-130` exposes the public registration overload family (`AddCosmosSnapshotStorageProvider()` plus connection-string, `Action<TOptions>`, and `IConfiguration` overloads) and defers bootstrap to a hosted service.
- **Evidence C**: `tests/Tributary.Runtime.Storage.Cosmos.L0Tests/SnapshotStorageProviderTests.cs:15-135` and `tests/Tributary.Runtime.Storage.Cosmos.L0Tests/SnapshotStorageProviderRegistrationsTests.cs:35-46` exercise the provider and DI shape as a stable baseline.
- **Conclusion**: The new provider should be planned as a sibling package that preserves the same consumer-facing registration ergonomics and testing expectations.
- **Confidence**: High.

## Finding 2: The shared snapshot abstraction is intentionally narrow and does not expose provider-specific concurrency concepts
- **Claim**: V1 can keep optimistic concurrency internal to the Blob provider without changing the shared abstraction.
- **Evidence A**: `src/Tributary.Runtime.Storage.Abstractions/ISnapshotStorageProvider.cs:3-14` only adds a `Format` identifier on top of the reader/writer contracts.
- **Evidence B**: `src/Tributary.Runtime.Storage.Abstractions/SnapshotStorageProviderExtensions.cs:10-70` only standardizes provider and options registration; it does not define extra concurrency hooks.
- **Conclusion**: Provider-specific write-conflict handling can be implemented behind the existing contract unless deeper evidence appears during implementation.
- **Confidence**: High.

## Finding 3: Mississippi storage providers already standardize on keyed SDK clients and hosted startup initialization
- **Claim**: The Blob provider should follow the existing keyed-client plus hosted-initializer pattern instead of introducing synchronous bootstrap or unkeyed storage clients.
- **Evidence A**: `src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs:50-67,132-185` registers keyed SDK handles and uses `IHostedService` for async initialization.
- **Evidence B**: `src/Brooks.Runtime.Storage.Cosmos/BrookStorageProviderRegistrations.cs:49-123,190-240` documents keyed Blob and Cosmos client expectations and uses the same sync-registration/async-startup model.
- **Evidence C**: `.github/instructions/keyed-services.instructions.md:11-18` and `.github/instructions/service-registration.instructions.md:13-19` codify keyed service usage and hosted-startup initialization as repository rules.
- **Conclusion**: The provider plan should include module-owned Blob service keys, provider-style registration overloads, and bootstrap work in a hosted service.
- **Confidence**: High.

## Finding 4: The repository already contains reusable Azurite and keyed Blob client patterns
- **Claim**: The new L2 plan should reuse existing Azurite fixture and keyed `BlobServiceClient` patterns rather than inventing new emulator wiring.
- **Evidence A**: `samples/Crescent/Crescent.L2Tests/CrescentFixture.cs:93-205` pre-registers keyed Blob and Cosmos clients and builds a host around emulator-backed resources.
- **Evidence B**: `samples/Crescent/Crescent.L2Tests/BlobStorageTests.cs:8-58` validates blob operations against Azurite, showing a current blob-emulator verification pattern already in the repo.
- **Evidence C**: `samples/Spring/Spring.AppHost/Program.cs:15-36` shows repo-consistent Aspire Azure Storage and Blob resource wiring.
- **Conclusion**: A Mississippi-owned Blob snapshot L2 test slice can be planned around an Aspire AppHost plus Azurite, borrowing from the Crescent/Spring patterns.
- **Confidence**: High.

## Finding 5: Mississippi test conventions support a dedicated L2 project with a companion AppHost
- **Claim**: A new Mississippi `Tributary.Runtime.Storage.Blob.L2Tests` project with an AppHost companion fits existing repo conventions.
- **Evidence A**: `.github/instructions/testing.instructions.md:13-18,66-68` requires L0-first testing, reserves L2 for real infrastructure such as Blob storage, and recommends a companion Aspire AppHost for L2 tests.
- **Evidence B**: `tests/Aqueduct.Gateway.L2Tests/Aqueduct.Gateway.L2Tests.csproj:1-25` and `tests/Aqueduct.Gateway.L2Tests.AppHost/Aqueduct.Gateway.L2Tests.AppHost.csproj:1-11` demonstrate the Mississippi-side L2-plus-AppHost project structure under `tests/`.
- **Conclusion**: The epic should decompose L0 and L2 work separately, with the L2 slice owning a test project and AppHost pair.
- **Confidence**: High.

## Finding 6: Tributary storage-provider documentation already has a placeholder overview and Cosmos provider page
- **Claim**: The documentation work should extend the existing Tributary storage-provider docs instead of creating a new top-level doc area.
- **Evidence A**: `docs/Docusaurus/docs/tributary/storage-providers/index.md:15-39` defines the storage-provider overview and currently lists only Cosmos DB.
- **Evidence B**: `docs/Docusaurus/docs/tributary/storage-providers/cosmos.md:15-59` is the existing provider-specific page shape that a Blob provider page can mirror.
- **Evidence C**: `docs/Docusaurus/docs/contributing/documentation-guide.md:71-109` defines the required frontmatter and closing-section rules that the new provider page must follow.
- **Conclusion**: The documentation slice should add a Blob provider page, update the overview page, and include at least one verified wiring example in the current Tributary docs area.
- **Confidence**: High.

## Finding 7: No existing repository-standard live Azure Blob smoke path was found for Tributary storage providers
- **Claim**: The exact delivery form of the required live Azure Blob smoke path remains an open planning decision.
- **Evidence A**: Repository search for `smoke`, `Azure Blob`, `live Azure`, and related patterns found emulator-backed blob tests and general smoke references, but no Tributary storage-provider live-cloud path.
- **Evidence B**: The located storage-provider and L2 assets (`samples/Crescent/Crescent.L2Tests/*`, `src/Tributary.Runtime.Storage.Cosmos/*`, `docs/Docusaurus/docs/tributary/storage-providers/*`) do not document or implement a live Azure Blob smoke workflow.
- **Conclusion**: The plan should ask for a decision or choose a repo-consistent default for how to express the live smoke requirement.
- **Confidence**: Medium.

## CoV Summary
- **Cross-cutting conclusion**: The repository strongly favors parity with the current Cosmos snapshot provider, keyed SDK client registrations, hosted startup initialization, Mississippi-owned L0/L2 verification, and Tributary-local provider documentation.
- **Two-source verification status**: Findings 1-6 are backed by at least two independent repo sources. Finding 7 is verified by negative evidence from multiple repo searches and remains the primary open decision.
- **Impact on planning**: The eventual epic should decompose into provider/package scaffolding, core Blob persistence behavior, emulator-backed L2 verification, and documentation/wiring/live-smoke completion slices.
