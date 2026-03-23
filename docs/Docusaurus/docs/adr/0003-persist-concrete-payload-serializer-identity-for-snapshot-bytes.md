---
title: "ADR-0003: Persist Concrete Payload Serializer Identity for Snapshot Bytes"
description: Resolve the snapshot payload serializer upstream and persist its concrete serializer identity with each stored snapshot blob.
sidebar_position: 3
status: "proposed"
date: 2026-03-23
decision_makers:
  - cs ADR Keeper
consulted:
  - Serialization architecture reviewer
  - Three Amigos synthesis
informed:
  - Tributary maintainers
  - Brooks serialization maintainers
---

# ADR-0003: Persist Concrete Payload Serializer Identity for Snapshot Bytes

## Context and Problem Statement

The Blob provider persists opaque snapshot payload bytes through the existing `SnapshotEnvelope` contract, so it cannot infer later which serializer produced those bytes. The architecture also needs restart-safe behavior when multiple Brooks serializers are registered and when the configured default serializer changes over time.

## Decision Drivers

- The provider must remain compatible with the existing `ISnapshotStorageProvider` and `SnapshotEnvelope` contract.
- Persisted blobs must stay readable after restart and after serializer-default changes.
- The design must distinguish serializer family selection from concrete persisted wire identity.
- Multiple registered serializers must not rely on DI registration order.

## Considered Options

- Resolve the serializer upstream and persist a concrete `payloadSerializerId` in the blob header.
- Persist only a format family name such as `json`.
- Move typed snapshot-state serialization responsibility into the Blob provider.

## Decision Outcome

Chosen option: "Resolve the serializer upstream and persist a concrete `payloadSerializerId` in the blob header", because it preserves the existing upstream payload boundary while making each stored blob self-describing enough to reject ambiguous or unsupported serializer matches on restore.

### Consequences

- Good, because restart and reload do not depend on ambient serializer defaults.
- Good, because `storageFormatVersion` stays reserved for outer-frame evolution instead of payload evolution.
- Good, because the provider can fail fast when a configured serializer format does not resolve uniquely.
- Bad, because composition must add an explicit serializer selector when more than one `ISerializationProvider` is registered.
- Bad, because serializer implementations now need stable persisted identities rather than casual format labels.

### Confirmation

Compliance will be confirmed by tests that prove configured serializer selection resolves uniquely, persisted `payloadSerializerId` values round-trip through the blob header, and readers reject unknown serializer identifiers without silently falling back to ambient defaults.

## Pros and Cons of the Options

### Resolve the serializer upstream and persist a concrete `payloadSerializerId` in the blob header

Keep typed snapshot serialization in Tributary composition, then record the resolved concrete serializer identity in persisted metadata.

- Good, because it respects the existing `SnapshotEnvelope` boundary.
- Good, because it keeps payload evolution separate from outer-frame evolution.
- Neutral, because configuration can still use friendly format names such as `json`.
- Bad, because the composition root becomes slightly more explicit when multiple serializers are registered.

### Persist only a format family name such as `json`

Record only the requested serializer family and assume it is sufficient for restore.

- Good, because configuration and metadata stay simple.
- Bad, because two incompatible serializers can truthfully identify as the same family.
- Bad, because restart safety depends on ambient serializer behavior rather than the stored blob.

### Move typed snapshot-state serialization responsibility into the Blob provider

Have the Blob provider serialize and deserialize typed snapshot state directly.

- Good, because the provider would know exactly which serializer it used.
- Bad, because it violates the existing `SnapshotEnvelope` storage boundary.
- Bad, because it would push serializer and domain concerns into the storage provider layer.

## More Information

- [Architecture Decision Records](index.md)
