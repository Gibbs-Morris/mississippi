# Requirements Synthesis

## Problem to Solve

Tributary needs a new Azure Blob Storage provider because the existing Cosmos-backed provider is constrained by practical payload-size limits around 4 MB, and some scenarios need materially larger stored artifacts.

## Desired Outcome

Build a new provider analogous to `Tributary.Runtime.Storage.Cosmos`, but backed by Azure Blob Storage and designed for larger payload support while preserving the existing contract and configuration experience as closely as practical.

## Confirmed Requirements

- Create a new Blob-backed Tributary storage provider that follows the same contracts as `Tributary.Runtime.Storage.Cosmos`.
- Preserve contract-level behavior and outward usage patterns, while allowing Blob-specific internals.
- Treat Blob as an app-level alternative provider, not a mandatory side-by-side mixed-provider design in v1.
- Use a one-logical-record-per-blob storage model.
- Use prefix listing plus naming conventions to locate the latest record and prune candidates in v1.
- Support provider-wide compression with an explicit option of either off or gzip in v1.
- Make payload serialization pluggable; default serializer should be JSON.
- Keep provider metadata in a fixed provider-owned stored envelope that carries items such as serializer identity, compression algorithm, and storage-format version.
- Mirror the Cosmos provider's registration and options patterns closely so adopters have a familiar setup experience.
- Provide comprehensive unit test coverage.
- Add a Crescent L2 happy path if feasible, and that L2 should exercise compression, one non-default configuration, persisted metadata expectations, and restart/reload compatibility.

## Explicitly Deferred or Not Requested

- No requirement yet for automatic compression thresholds.
- No requirement yet for per-artifact compression policies.
- No requirement yet for pluggable provider metadata formats.
- No requirement yet for manifest/index blobs or blob-tag based lookup.
- No requirement yet for mixed Blob-and-Cosmos operation inside one app workflow.

## Working Assumptions for Architecture

- The public API should remain as close as possible to the existing Cosmos provider unless Blob-specific constraints make that unsafe or misleading.
- Blob storage is being introduced primarily to increase supported payload size, not to redesign the broader Tributary storage abstraction.
- First release should optimize for correctness, clarity, and repo consistency ahead of advanced Blob-specific optimizations.

## Acceptance Direction

The implementation should be considered successful when a framework adopter can configure the Blob provider through familiar options/DI patterns, store larger Tributary payloads with optional gzip compression, select a payload serializer with JSON as default, and verify the behavior through strong L0 coverage plus a meaningful Crescent L2 scenario.