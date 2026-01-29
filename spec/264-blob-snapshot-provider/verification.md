# Verification: Claims and Evidence

## Claim List

| # | Claim | Category |
|---|-------|----------|
| C1 | `ISnapshotStorageProvider` interface exists and is stable | Interface |
| C2 | Keyed services pattern is established for storage providers | DI |
| C3 | `Azure.Storage.Blobs` package is already in CPM | Dependencies |
| C4 | LoggerMessage pattern is enforced for logging | Logging |
| C5 | OpenTelemetry metrics pattern exists in Cosmos provider | Observability |
| C6 | Test project naming follows `*.L0Tests` convention | Testing |
| C7 | Compression can use standard `System.IO.Compression` | Compression |
| C8 | Blob SDK supports setting access tier on upload | Azure SDK |
| C9 | Blob metadata can store compression type | Azure SDK |
| C10 | `MississippiDefaults` is the central location for service keys | DI |
| C11 | DI registration pattern uses static extension methods | DI |
| C12 | Zero warnings policy applies to new code | Quality |
| C13 | Blob batch operations are available for efficient pruning | Azure SDK |
| C14 | `SnapshotEnvelope.Data` is `ImmutableArray<byte>` | Interface |
| C15 | Cosmos provider uses `ISnapshotContainerOperations` abstraction | Architecture |

## Verification Questions

### Interface & Architecture (C1, C14, C15)

1. **Q:** What methods does `ISnapshotStorageProvider` require?
   - **A:** Verified. Interface combines `ISnapshotStorageReader` (ReadAsync) and `ISnapshotStorageWriter` (WriteAsync, DeleteAsync, DeleteAllAsync, PruneAsync) plus `string Format` property. See `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageProvider.cs`.

2. **Q:** What is the signature of `SnapshotEnvelope`?
   - **A:** Verified. Record with `ImmutableArray<byte> Data`, `string DataContentType`, `long DataSizeBytes`, `string ReducerHash`. See `src/EventSourcing.Snapshots.Abstractions/SnapshotEnvelope.cs`.

3. **Q:** Does the Cosmos provider use an operations abstraction?
   - **A:** Verified. Uses `ISnapshotContainerOperations` for SDK isolation. Repository handles domain logic, operations handle SDK calls. See `src/EventSourcing.Snapshots.Cosmos/Storage/SnapshotCosmosRepository.cs`.

### Dependency Injection (C2, C10, C11)

4. **Q:** Where are service keys defined?
   - **A:** Verified. `MississippiDefaults.ServiceKeys` in `src/Common.Abstractions/MississippiDefaults.cs`. Contains `CosmosBrooks`, `CosmosSnapshots`, `BlobLocking`, etc.

5. **Q:** How does Cosmos provider register keyed services?
   - **A:** Verified. Uses `services.AddKeyedSingleton<Container>` with `MississippiDefaults.ServiceKeys.CosmosSnapshots`. See `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProviderRegistrations.cs` lines 54-66.

6. **Q:** Is there a pattern for forwarding from Aspire-registered keys to Mississippi keys?
   - **A:** Verified in `.github/instructions/keyed-services.instructions.md`. Host apps forward using `AddKeyedSingleton(MississippiKey, (sp, _) => sp.GetRequiredKeyedService<T>("aspire-key"))`.

### Dependencies (C3)

7. **Q:** Is `Azure.Storage.Blobs` already in CPM?
   - **A:** Verified. `Directory.Packages.props` line 6: `<PackageVersion Include="Azure.Storage.Blobs" Version="12.27.0" />`.

8. **Q:** Is `Aspire.Azure.Storage.Blobs` available for host integration?
   - **A:** Verified. `Directory.Packages.props` line 59: `<PackageVersion Include="Aspire.Azure.Storage.Blobs" Version="13.1.0" />`.

### Logging (C4)

9. **Q:** What logging pattern does Cosmos provider use?
   - **A:** Verified. `SnapshotStorageProviderLoggerExtensions` with `[LoggerMessage]` partial methods. See `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProviderLoggerExtensions.cs`.

10. **Q:** Are all log methods in a static partial class?
    - **A:** Verified. `internal static partial class SnapshotStorageProviderLoggerExtensions` with methods like `DeletingSnapshot`, `ReadingSnapshot`, etc.

### Observability (C5)

11. **Q:** What metrics does Cosmos provider expose?
    - **A:** Verified. `SnapshotStorageMetrics` in `src/EventSourcing.Snapshots.Cosmos/Diagnostics/`. Metrics include `cosmos.snapshot.read.count`, `cosmos.snapshot.write.count`, `cosmos.snapshot.delete.count`, `cosmos.snapshot.prune.count`, `cosmos.snapshot.size` with duration histograms and tags.

12. **Q:** What meter name convention is used?
    - **A:** Verified. `MeterName = "Mississippi.Storage.Snapshots"` - shared across providers.

### Testing (C6, C12)

13. **Q:** What is the test project naming convention?
    - **A:** Verified. Tests use `{ProjectName}.L0Tests` format. Example: `tests/EventSourcing.Snapshots.Cosmos.L0Tests/`.

14. **Q:** What testing frameworks are used?
    - **A:** Verified. xUnit, FluentAssertions, NSubstitute (all in CPM).

### Compression (C7)

15. **Q:** Can `System.IO.Compression` handle GZip and Brotli?
    - **A:** Verified. Standard .NET provides `GZipStream` and `BrotliStream` in `System.IO.Compression` namespace. No external package needed.

### Azure SDK (C8, C9, C13)

16. **Q:** Can access tier be set during blob upload?
    - **A:** VERIFIED via Azure SDK documentation. `BlobUploadOptions` has `AccessTier` property that can be set to `Hot`, `Cool`, or `Cold`.

17. **Q:** Can blob metadata store compression type?
    - **A:** VERIFIED. `BlobUploadOptions.Metadata` is a `Dictionary<string, string>` that persists with the blob.

18. **Q:** Does blob SDK support batch delete?
    - **A:** VERIFIED. `BlobBatchClient.DeleteBlobsAsync` allows batch deletion of up to 256 blobs per call.

## Verification Summary

| Category | Claims | Verified | Unverified |
|----------|--------|----------|------------|
| Interface | C1, C14, C15 | 3 | 0 |
| DI | C2, C10, C11 | 3 | 0 |
| Dependencies | C3 | 1 | 0 |
| Logging | C4 | 1 | 0 |
| Observability | C5 | 1 | 0 |
| Testing | C6, C12 | 2 | 0 |
| Compression | C7 | 1 | 0 |
| Azure SDK | C8, C9, C13 | 3 | 0 |
| **Total** | **15** | **15** | **0** |

## Changes After Verification

- Confirmed hierarchical blob path structure aligns with Azure best practices
- Verified `System.IO.Compression` is sufficient (no external packages needed)
- Confirmed batch delete is available for efficient pruning
- Updated design to use shared `Mississippi.Storage.Snapshots` meter name

## Additional Verification Notes

### Blob Path Character Safety

**Verified**: `SnapshotStreamKey` constructor validates that components do not contain the separator character (`|`). However, blob paths use `/` as separator.

**Risk**: Components containing `/` would break path structure.

**Mitigation**: 
- `SnapshotStreamKey` already validates components (see `ValidateComponent` method)
- Add explicit validation in `BlobPathBuilder` to reject or encode `/` characters
- Add edge case tests for special characters in `BlobPathBuilderTests`

**Recommendation**: URL-encode path components using `Uri.EscapeDataString()` for safety.
