# Increment 6 Commit Review

## Verdict

Commit-safe as a small focused increment.

The two prior blockers are resolved in the current uncommitted slice:

- the public connection-string overload now registers the keyed Blob client under the effective `BlobServiceClientServiceKey` selected by `configureOptions`;
- the Crescent trust slice now proves Blob-backed restart hydration explicitly by overwriting the persisted snapshot blob with a different marker and payload before restart, then asserting the restarted aggregate hydrates that Blob-only state rather than the original event-stream state.

## Focused Review Scope

Reviewed against the current working tree with emphasis on the prior blockers only:

- `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs`
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs`
- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs`
- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs`
- `samples/Crescent/Crescent.L2Tests/Crescent.L2Tests.csproj`
- `samples/Crescent/Crescent.L2Tests/packages.lock.json`

## Validation Summary

Evidence for the connection-string overload honesty:

- `SnapshotBlobStorageProviderRegistrations.cs` now computes `effectiveOptions` before registering the keyed `BlobServiceClient`, and the registration uses `effectiveOptions.BlobServiceClientServiceKey` rather than the default key.
- `SnapshotBlobStorageProviderRegistrationsTests.cs` now includes `AddBlobSnapshotStorageProviderWithConnectionStringShouldHonorBlobServiceClientServiceKeyOverride`, which exercises the non-default key path and resolves both the custom-keyed `BlobServiceClient` and the derived `BlobContainerClient` successfully.

Evidence for the Crescent Blob restart proof:

- `BlobSnapshotTrustSliceScenario.cs` uses the public connection-string overload in the sample host, so the trust slice exercises the same public API surface being exposed by this increment.
- `BlobSnapshotTrustSliceTests.cs` now:
	- writes a large snapshot through the canonical registration path;
	- locates and inspects the persisted blob frame;
	- overwrites that blob with a different marker and payload that do not exist in the event stream;
	- restarts Orleans; and
	- asserts the restarted aggregate rehydrates the Blob-only marker and payload instead of the original event-backed values.

That assertion shape is strong enough: if restart hydration regressed to Cosmos replay, the test would observe the original marker and payload, not the overwritten Blob state.

## Test Verification

Commands run:

```powershell
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release --filter SnapshotBlobStorageProviderRegistrationsTests
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests
```

Observed results:

- Blob registration L0 tests passed: 13/13.
- Crescent trust-slice L2 test passed: 1/1 in about 43.5s.
- The passing L2 run again showed Blob snapshot activity before restart and a successful post-overwrite restart path, consistent with the stronger test assertions.

## Remaining Material Issues

None for the two previously blocking concerns.

## Recommendation

This slice is acceptable to commit as increment 6. The change remains focused: it exposes the Blob snapshot registration surface for external consumers, adds direct coverage for the keyed-client override behavior, and backs that API with a Crescent trust slice that now explicitly distinguishes Blob reload from event replay.