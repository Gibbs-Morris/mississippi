# Increment 1 Changes

## Implementation Summary

- Kept the new `Mississippi.Tributary.Runtime.Storage.Blob` project and `Mississippi.Tributary.Runtime.Storage.Blob.L0Tests` project wired into `mississippi.slnx`.
- Narrowed the increment-1 setup surface from public to internal so the assembly does not advertise a usable provider while storage operations still throw for all runtime paths.
- Kept the four registration overloads as internal increment-1 scaffolding:
  - parameterless registration for externally supplied keyed `BlobServiceClient`
  - connection string overload with optional options action
  - `Action<SnapshotBlobStorageOptions>` overload
  - `IConfiguration` overload
- Preserved only the options that drive observable increment-1 behavior:
  - `ContainerInitializationMode`
  - `ContainerName`
  - `BlobServiceClientServiceKey`
  - `PayloadSerializerFormat`
- Removed inert increment-1 options that were previously validated but not honored by behavior:
  - `BlobPrefix`
  - `Compression`
  - `ListPageSizeHint`
  - `MaximumHeaderBytes`
- Reduced the options validator accordingly so increment 1 only validates the currently meaningful setup knobs.
- Kept the working startup scaffolding intact:
  - keyed `BlobContainerClient` registration under `mississippi-blob-snapshots`
  - `SnapshotPayloadSerializerResolver` for exact serializer resolution by `ISerializationProvider.Format`
  - `BlobContainerInitializer` hosted service for serializer validation and container initialization mode handling
  - `BlobContainerInitializerOperations` as the Azure Blob SDK seam used only for startup scaffolding
- Kept the minimal `SnapshotBlobStorageProvider` shell internal with `Format = "azure-blob"` and explicit `NotSupportedException` throws until later increments add real storage behavior.

## Tests Added

- Updated defaults and options validation tests in `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs` to match the narrowed increment-1 options surface.
- Expanded registration and startup validation tests in `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs` to cover all four registration overloads plus startup behavior:
  - parameterless registration
  - connection string overload
  - `Action<SnapshotBlobStorageOptions>` overload
  - `IConfiguration` overload
  - missing keyed Blob client failure
  - zero serializer match failure
  - multiple serializer match failure
  - `CreateIfMissing` startup path
  - `ValidateExists` missing-container failure
  - `ValidateExists` success path
- Moved the registration test helper types into dedicated top-level test files to satisfy repo guidance and StyleCop rules.

## Verification Evidence

- Commands run:

```powershell
dotnet build .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release
```

- Result: build succeeded with 0 warnings; 13 tests passed, 0 failed.

## Risks And Mitigations

- Current serializer validation still uses `ISerializationProvider.Format` because the shared abstraction does not yet expose a persisted serializer identity. This is sufficient for increment-1 startup validation but must be tightened when the stored frame is introduced.
- The provider shell remains intentionally non-functional for read/write/delete/prune. The mitigation in this remediation is that the registration and configuration surface is now internal instead of misleadingly public.
- Container initialization still validates startup behavior only; it does not yet enforce Blob-frame, naming, or repository invariants.

## Follow-Up For Increment 2

- Implement canonical stream identity and bounded blob naming.
- Add conditional-create semantics for duplicate-version conflict handling.
- Add stream-local listing primitives and tests for version ordering and cross-stream isolation.
- Decide whether any additional startup diagnostics are needed once naming primitives exist.