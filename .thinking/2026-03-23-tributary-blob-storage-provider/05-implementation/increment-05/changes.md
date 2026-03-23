# Increment 5 Changes

## Implementation Summary

- Hardened Blob startup validation in `BlobContainerInitializer` so serializer-resolution failures and container operation failures are rethrown as actionable startup exceptions that include the container name, initialization mode, and next-step guidance.
- Preserved the existing `CreateIfMissing` and `ValidateExists` behaviors while making `ValidateExists` missing-container failures explicitly name the configured mode.
- Added startup failure logging through `SnapshotBlobStartupLoggerExtensions` for:
  - serializer validation failures
  - container initialization or validation failures
- Added provider-boundary diagnostics in `SnapshotBlobStorageProvider` so:
  - duplicate-version write conflicts are logged and rethrown without changing the exception type
  - unreadable stored blobs are logged with the unreadable-frame reason and rethrown without being mistaken for missing snapshots
- Strengthened `SnapshotBlobDuplicateVersionException` with a clearer message that explains Blob snapshot storage does not overwrite an existing version.

## Tests Added Or Updated

- Updated `SnapshotBlobStorageProviderRegistrationsTests` to verify:
  - serializer startup failures now surface startup-context diagnostics
  - serializer startup failures emit the expected error log event, level, and structured startup context
  - `ValidateExists` missing-container failures mention the configured initialization mode
  - `CreateIfMissing` operational failures are wrapped with actionable startup guidance and preserve the inner exception
  - `CreateIfMissing` operational failures emit the expected startup failure error log with container and mode context
  - `ValidateExists` operational failures are wrapped with actionable startup guidance and preserve the inner exception
  - `ValidateExists` operational failures emit the expected startup failure error log with container and mode context
- Updated `SnapshotBlobStorageProviderTests` to verify:
  - duplicate-version conflicts surface actionable diagnostics through the provider boundary
  - duplicate-version conflicts emit the expected warning log with the conflicting snapshot key
  - unreadable stored blobs surface `SnapshotBlobUnreadableFrameException` through the provider boundary with the expected reason
  - unreadable stored blobs emit the expected error log with snapshot key and unreadable-frame reason
- Added `TestLogger` and `TestLogEntry` helpers to capture structured logs in Blob L0 tests without changing production code.
- Updated `StubBlobContainerInitializerOperations` to simulate create and exists operation failures for startup diagnostics coverage.

## Verification Evidence

- Commands run:

```powershell
dotnet build .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
```

- Results:
  - Build succeeded with zero warnings and zero errors.
  - Blob L0 tests succeeded: 72 passed, 0 failed, 0 skipped.

## Risks And Mitigations

- Startup operational failures are now normalized to `InvalidOperationException` with preserved inner exceptions rather than inventing a new exception family. This keeps the host-startup behavior stable while improving operator guidance.
- Provider diagnostics remain log-and-rethrow. This preserves the current contract and failure types while making duplicate conflicts and unreadable blobs visible to callers and logs.

## Blockers

- None currently, assuming the targeted build and test rerun stays green.

## Follow-On Work For Increment 6

- Add the focused Azurite-backed Crescent L2 trust slice for canonical registration, restart, gzip read-back, and non-default serializer restart survival.
- Reuse the increment-5 diagnostic expectations as the operator-facing baseline in the L2 scenario rather than expanding the exception surface further.