---
title: "ADR-0001: Use Canonical Stream Identity and Hashed Blob Naming for Snapshot Blobs"
description: Use a provider-owned canonical stream identity, hashed stream prefixes, and zero-padded versioned blob names for Tributary snapshot blobs.
sidebar_position: 1
status: "proposed"
date: 2026-03-23
decision_makers:
  - cs ADR Keeper
consulted:
  - Three Amigos synthesis
  - Cloud architecture reviewer
  - Serialization architecture reviewer
informed:
  - Tributary maintainers
  - Documentation maintainers
---

# ADR-0001: Use Canonical Stream Identity and Hashed Blob Naming for Snapshot Blobs

## Context and Problem Statement

The Blob snapshot provider needs persisted blob names that are bounded, deterministic, and safe for stream-local maintenance operations such as delete-all and prune. The architecture also needs to avoid turning `SnapshotStreamKey.ToString()` into a durable storage contract because string formatting changes would silently change persisted identities and listing behavior.

## Decision Drivers

- Blob names must stay bounded even when logical stream keys are long.
- Delete-all and prune must remain scoped to one logical stream.
- Lexical ordering must preserve version ordering during prefix scans.
- Persisted stream identity must remain stable independently from display formatting.

## Considered Options

- Use a provider-owned canonical stream identity with a hashed stream prefix and zero-padded versioned blob names.
- Use `SnapshotKey.ToString()` or `SnapshotStreamKey.ToString()` directly as the blob name or prefix.
- Use a human-readable stream identity directly in the blob path without hashing.

## Decision Outcome

Chosen option: "Use a provider-owned canonical stream identity with a hashed stream prefix and zero-padded versioned blob names", because it gives the provider a stable persisted naming contract, keeps blob names bounded, and preserves stream-local ordering without relying on casual string formatting.

### Consequences

- Good, because stream prefixes remain bounded and deterministic regardless of stream-key length.
- Good, because delete-all and prune can enumerate only one stream prefix instead of scanning broader container state.
- Good, because zero-padded version suffixes preserve lexical ordering for stream-local scans.
- Bad, because operators cannot infer the stream identity from the blob path alone.
- Bad, because latest-by-stream discovery remains a linear scan across the stream prefix rather than an indexed lookup.

### Confirmation

Compliance will be confirmed by code review and tests that prove canonical stream identity does not depend on `ToString()`, blob names remain bounded, and prefix enumeration stays scoped to the hashed stream prefix for maintenance operations.

## Pros and Cons of the Options

### Use a provider-owned canonical stream identity with a hashed stream prefix and zero-padded versioned blob names

Persist a stable canonical stream identity, derive the blob prefix as `{BlobPrefix}{Sha256Hex(canonicalStreamIdentity)}/`, and derive the blob name as `{streamPrefix}v{version:D20}.snapshot`.

- Good, because the storage contract is provider-owned and intentionally stable.
- Good, because naming stays safe when stream keys are long or contain awkward characters.
- Neutral, because human-readable identity moves from the path into the header and optional diagnostics.
- Bad, because stream history still requires linear prefix scanning for latest or prune operations.

### Use `SnapshotKey.ToString()` or `SnapshotStreamKey.ToString()` directly as the blob name or prefix

Make existing string representations the durable naming contract.

- Good, because blob paths are easy for humans to read.
- Bad, because formatting changes would silently change the persisted storage contract.
- Bad, because long or unusual string values can create unbounded or awkward blob names.

### Use a human-readable stream identity directly in the blob path without hashing

Persist a canonical but readable stream identity directly in the path.

- Good, because container browsing is more operator-friendly.
- Good, because the provider still owns the canonical representation.
- Bad, because long stream identities still produce long blob names and prefixes.
- Bad, because the design loses the hard bound that the hash-based prefix provides.

## More Information

- [Architecture Decision Records](index.md)
