---
title: "ADR-0004: Compress Only Snapshot Payload Bytes and Verify Payload Integrity"
description: Apply optional compression only to the payload segment and persist integrity metadata for the uncompressed payload bytes.
sidebar_position: 4
status: "proposed"
date: 2026-03-23
decision_makers:
  - cs ADR Keeper
consulted:
  - Cloud architecture reviewer
  - Serialization architecture reviewer
informed:
  - Tributary maintainers
  - Operations maintainers
---

# ADR-0004: Compress Only Snapshot Payload Bytes and Verify Payload Integrity

## Context and Problem Statement

The Blob provider must support larger payloads and optional provider-wide compression without making stored metadata opaque or operationally misleading. The architecture also needs a way to distinguish corruption from codec mismatch or serializer mismatch when decoding stored blobs.

## Decision Drivers

- Metadata must remain readable without decompressing the full blob.
- Compression must support `off` and `gzip` without misrepresenting the blob as wholly gzip-encoded.
- Decode failures need a clear integrity boundary over the original payload bytes.
- Payload bytes must round-trip exactly as `SnapshotEnvelope.Data` produced them upstream.

## Considered Options

- Compress only the payload segment, persist the compression algorithm in the header, and verify integrity with a checksum over the uncompressed payload bytes.
- Compress the entire blob body.
- Do not compress and rely only on raw blob storage.

## Decision Outcome

Chosen option: "Compress only the payload segment, persist the compression algorithm in the header, and verify integrity with a checksum over the uncompressed payload bytes", because it preserves header inspectability, avoids misleading whole-blob encoding semantics, and improves failure diagnosis when stored payload bytes cannot be restored.

### Consequences

- Good, because the header stays inspectable even when `gzip` is enabled.
- Good, because the provider can reject checksum mismatches before handing bytes back to Tributary.
- Good, because the provider does not need to set `Content-Encoding` for a partially compressed custom frame.
- Bad, because encode and decode paths must maintain an explicit payload boundary rather than treating the blob as a single compressed document.
- Bad, because checksum computation adds some extra work during write and read.

### Confirmation

Compliance will be confirmed by tests that round-trip both compression modes, verify the checksum across uncompressed payload bytes, reject corrupt compressed payloads deterministically, and assert that no misleading whole-blob `Content-Encoding` is emitted.

## Pros and Cons of the Options

### Compress only the payload segment, persist the compression algorithm in the header, and verify integrity with a checksum over the uncompressed payload bytes

Limit compression to `PayloadBytes` while keeping header metadata uncompressed and authoritative.

- Good, because metadata remains readable and restart-safe.
- Good, because payload corruption is easier to distinguish from serializer or codec mismatch.
- Neutral, because the provider can add future codecs without changing the overall framing pattern.
- Bad, because the codec boundary is more explicit than a blanket whole-blob compression approach.

### Compress the entire blob body

Apply compression to the full blob representation.

- Good, because the implementation path looks simpler.
- Bad, because metadata is no longer inspectable without decompression.
- Bad, because `Content-Encoding` semantics become misleading when the stored blob is also a custom provider frame.

### Do not compress and rely only on raw blob storage

Persist the header and payload without any compression support.

- Good, because the format stays simpler.
- Good, because encode and decode paths do less work.
- Bad, because large payload scenarios lose the v1 compression option that the design explicitly requires.

## More Information

- [Architecture Decision Records](index.md)
