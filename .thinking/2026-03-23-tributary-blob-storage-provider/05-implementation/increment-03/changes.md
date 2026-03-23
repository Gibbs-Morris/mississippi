# Increment 3 Changes

## Implementation Summary

- Added increment-3 Blob frame configuration to `SnapshotBlobStorageOptions` and `SnapshotBlobDefaults`:
  - `Compression` with default `Off`
  - `MaxHeaderBytes` with default `65536`
- Extended `SnapshotBlobStorageOptionsValidator` to reject unsupported compression values and non-positive `MaxHeaderBytes` values.
- Extended `SnapshotPayloadSerializerResolver` with a concrete persisted serializer identity path via `ResolveConfiguredSerializerDescriptor()` and `GetSerializerId(...)`.
- Registered `TimeProvider.System` and `IBlobEnvelopeCodec` in `SnapshotBlobStorageProviderRegistrations` so the frame path is available without implementing increment-4 repository/provider reads.
- Added the internal stored-frame codec stack under `src/Tributary.Runtime.Storage.Blob/Storage/`:
  - `IBlobEnvelopeCodec`
  - `BlobEnvelopeCodec`
  - `StoredSnapshotBlobHeader`
  - `DecodedSnapshotBlobFrame`
  - `SnapshotBlobUnreadableFrameReason`
  - `SnapshotBlobUnreadableFrameException`
- Implemented the provider-owned stored Blob frame with:
  - fixed 16-byte prelude
    - 8-byte ASCII magic: `TRIBSNAP`
    - 2-byte little-endian frame version
    - 2-byte reserved flags
    - 4-byte little-endian header length
  - bounded UTF-8 JSON header
  - payload segment stored uncompressed or gzip-compressed
- Implemented authoritative stored header metadata for:
  - `storageFormatVersion`
  - `canonicalStreamIdentity`
  - `version`
  - `snapshotStorageName`
  - `reducerHash`
  - `dataContentType`
  - `payloadSerializerId`
  - `compressionAlgorithm`
  - `payloadSha256`
  - `uncompressedPayloadBytes`
  - `storedPayloadBytes`
  - `writtenUtc`
- Implemented fail-closed decode behavior for:
  - truncated prelude
  - invalid magic
  - unsupported frame version
  - unsupported flags
  - invalid or oversized header length
  - malformed header JSON
  - missing/invalid required header values
  - unexpected stream identity or version
  - unknown persisted serializer identity
  - unknown compression algorithm
  - invalid stored payload length
  - invalid compressed payload
  - invalid uncompressed payload length
  - payload checksum mismatch
- Kept repository/provider behavior intentionally scoped to increment 3:
  - no new public storage contract changes
  - no exact-read/latest-read repository integration yet
  - no delete/prune/provider operation implementation beyond the existing increment-2 seams

## Tests Added Or Updated

- Updated `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs` for the new increment-3 defaults and validation rules.
- Updated `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs` to assert `IBlobEnvelopeCodec` registration.
- Updated `tests/Tributary.Runtime.Storage.Blob.L0Tests/TestSerializationProvider.cs` to allow alternate concrete serializer test types.
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/AlternateTestSerializationProvider.cs` to prove persisted concrete serializer identity survives across codec instances and fails when the current process no longer recognizes that id.
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs` covering:
  - round-trip encode/decode for `Off` and `Gzip`
  - persisted concrete serializer identity
  - unknown serializer failure
  - truncated prelude failure
  - invalid magic failure
  - unsupported frame version failure
  - unsupported flags failure
  - invalid or oversized header length failure, including persisted values above `Int32.MaxValue`
  - malformed header failure
  - invalid required header values failure
  - snapshot identity mismatch failure
  - unknown compression algorithm failure
  - invalid stored payload length failure
  - invalid compressed payload failure
  - invalid uncompressed payload length failure
  - checksum mismatch failure
  - truncated gzip payload failure
  - deterministic large-payload matrix for `256 KB`, `1 MB`, `5 MB`, and `16 MB`

## Verification Evidence

- Commands run:

```powershell
dotnet build .\src\Tributary.Runtime.Storage.Blob\Tributary.Runtime.Storage.Blob.csproj -c Release -warnaserror
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror --filter "FullyQualifiedName~LargePayloadMatrixShouldRoundTripDeterministicPayloadSizes" --logger "console;verbosity=detailed"
```

- Results:
  - Blob provider project build succeeded with zero warnings.
  - Blob L0 test project succeeded with zero warnings.
  - Full Blob L0 summary: 53 passed, 0 failed, 0 skipped.
  - Large-payload matrix summary: 4 passed, 0 failed.

## Risks And Mitigations

- The persisted serializer identity currently derives from the serializer provider CLR type name because `ISerializationProvider` exposes only `Format`. The new tests pin that identity behavior and verify unknown persisted ids fail closed.
- The stored header now includes `dataContentType` even though the architecture notes emphasized serializer/compression/checksum metadata. This keeps the Blob frame lossless for `SnapshotEnvelope` reconstruction without broadening increment-4 repository behavior.
- The current evidence matrix uses a deterministic, highly compressible payload pattern, so gzip frame sizes are much smaller than raw payload sizes. The artifact records the buffering model and the measured stored-frame bytes explicitly so the evidence is reproducible and reviewable.

## Follow-On Work For Increment 4

- Integrate `IBlobEnvelopeCodec` into repository write and read paths for exact read and latest read.
- Implement provider-wide `Off` and `Gzip` repository behavior on top of the codec rather than at the test seam only.
- Ensure maintenance paths remain name-driven and do not download non-selected candidate payload bodies.
- Add non-default serializer read-path coverage through the real repository/provider integration rather than the codec seam alone.
