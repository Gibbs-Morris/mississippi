# Increment 6 Changes

## Scope completed

Implemented one focused Azurite-backed Crescent L2 trust slice for the new Blob snapshot provider. The slice proves:

- Crescent can call the canonical Blob registration extension from another assembly.
- The public connection-string registration overload honestly honors the effective `BlobServiceClientServiceKey` option.
- Brooks event storage remains Cosmos-backed while snapshots persist to Blob.
- A materially large aggregate snapshot is written and stored with gzip enabled.
- The persisted snapshot header records a non-default serializer identity.
- Restarting Orleans reads back the persisted Blob snapshot rather than merely replaying the original Cosmos event stream.

## Code changes

### Blob provider public registration surface

Made the minimum Blob registration/configuration types public so the sample can use the production extension methods directly:

- `SnapshotBlobStorageProviderRegistrations`
- `SnapshotBlobStorageOptions`
- `SnapshotBlobCompression`
- `SnapshotBlobContainerInitializationMode`

This keeps increment 6 aligned with the planned “canonical registration path” proof instead of introducing a sample-only backdoor.

Adjusted the public connection-string overload so it registers the keyed `BlobServiceClient` under the effective configured `BlobServiceClientServiceKey` before the provider resolves the keyed container client. Added focused L0 coverage for the override path.

### Crescent L2 trust slice

Added a dedicated trust-slice host and test path under `samples/Crescent/Crescent.L2Tests/`:

- `BlobSnapshotTrustSliceScenario.cs`
  - Starts the existing Crescent AppHost infrastructure.
  - Keeps Brooks on Cosmos.
  - Configures snapshots through `AddBlobSnapshotStorageProvider(blobConnectionString, options => ...)`.
  - Enables gzip and a non-default serializer format.
  - Supports Orleans restart without tearing down Azurite/Cosmos.
- `CrescentBlobCustomJsonSerializationProvider.cs`
  - Test-local JSON-compatible serializer with format `Crescent.CustomJson`.
  - Gives the persisted snapshot a stable non-default serializer identity.
- `LargeSnapshotAggregate.cs`
- `StoreLargeSnapshotCommand.cs`
- `LargeSnapshotStored.cs`
- `StoreLargeSnapshotCommandHandler.cs`
- `LargeSnapshotStoredEventReducer.cs`
- `LargeSnapshotScenarioRegistrations.cs`
  - Minimal aggregate path used only by the trust slice.
- `BlobSnapshotTrustSliceTests.cs`

  - Writes a large payload.
  - Polls Azurite for the snapshot blob.
  - Parses the raw Blob frame header.
  - Asserts gzip metadata and gzip payload prefix.
  - Asserts the persisted serializer id matches the custom provider type.
  - Rewrites the persisted Blob snapshot to a state that never existed in Cosmos.
  - Restarts Orleans and proves the restarted aggregate reads back the rewritten Blob snapshot.

Also added the Blob provider project reference to `samples/Crescent/Crescent.L2Tests/Crescent.L2Tests.csproj`.

## Validation

Commands run:

```powershell
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release --filter SnapshotBlobStorageProviderRegistrationsTests
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests
```

Results:

- Blob registration L0 tests passed: 13/13.
- Crescent Blob trust-slice L2 test passed: 1/1.

Notable automated evidence from the passing validation runs:

- The Blob registration tests now cover a non-default `BlobServiceClientServiceKey` override through the public connection-string overload.
- The trust slice rewrites the persisted snapshot blob after the initial write and before restart.
- After Orleans restart, the aggregate comes back with the rewritten Blob marker and payload, which do not exist in the original Cosmos event stream.

## Issues encountered and resolved

- The first large payload exceeded the Cosmos event-size limit for a single Brook event. I reduced the payload to a still-material size that stays under the Brook/Cosmos event limit while remaining well above the large-snapshot assertion threshold.
- The scenario initially disposed the Aspire testing builder too early, which tore down the emulator-backed infrastructure before restart validation. The scenario now retains the builder for the full test lifetime and disposes it during teardown.
- The newly public connection-string overload originally hard-coded the default keyed `BlobServiceClient`, making the `BlobServiceClientServiceKey` option dishonest on that path. The overload now registers the client under the effective configured key, and targeted L0 coverage locks that behavior in.
- The first trust-slice assertion set only proved restart survival. The test now rewrites the stored Blob snapshot to a state that Cosmos replay cannot produce, so the restart assertion fails closed if Blob-backed hydration regresses.

## Follow-on for increment 7

Increment 6 intentionally stays at one focused Crescent trust slice. Natural next work for increment 7 remains broader coverage and documentation, not more scope in this slice.
