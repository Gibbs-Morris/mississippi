---
title: "ADR-0002: Store Snapshots in a Versioned Self-Describing Blob Frame"
description: Store each snapshot as a provider-owned blob frame with a fixed binary prelude, an uncompressed JSON header, and a payload segment.
sidebar_position: 2
status: "proposed"
date: 2026-03-23
decision_makers:
  - cs ADR Keeper
consulted:
  - Cloud architecture reviewer
  - Serialization architecture reviewer
informed:
  - Tributary maintainers
  - Documentation maintainers
---

# ADR-0002: Store Snapshots in a Versioned Self-Describing Blob Frame

## Context and Problem Statement

The Blob provider stores opaque `SnapshotEnvelope.Data` bytes, so the persisted record needs its own durable framing and metadata rather than relying on ambient configuration or Azure-specific metadata features. The team also needs a format that remains inspectable after restart, survives serializer or compression default changes, and can fail closed when readers encounter unsupported formats.

## Decision Drivers

- The provider must own a durable persisted format independent from the upstream snapshot payload bytes.
- Persisted metadata must remain authoritative even when Azure metadata or tags are unavailable or omitted.
- The format must support strict validation and explicit forward-compatibility rules.
- Operators need metadata that remains inspectable without decoding the payload first.

## Considered Options

- Store each snapshot as a provider-owned frame with a fixed binary prelude, an uncompressed JSON header, and a payload segment.
- Store the entire snapshot record as a plain JSON document.
- Store compatibility metadata only in Azure blob metadata or blob index tags.

## Decision Outcome

Chosen option: "Store each snapshot as a provider-owned frame with a fixed binary prelude, an uncompressed JSON header, and a payload segment", because it gives the provider a precise durable contract, keeps compatibility metadata authoritative and inspectable, and lets the payload remain opaque bytes produced upstream.

### Consequences

- Good, because the provider can validate magic bytes, frame version, flags, and header length before attempting payload decode.
- Good, because the header remains readable and self-describing even when payload compression is enabled.
- Good, because Azure blob metadata and tags remain optional diagnostics instead of correctness dependencies.
- Bad, because the provider must maintain a custom outer format instead of relying on a simpler document shape.
- Bad, because additive evolution now requires explicit rules for required versus optional header fields.

### Confirmation

Compliance will be confirmed by tests that round-trip the binary prelude and header, reject unsupported frame versions and invalid flags, enforce maximum header size, and prove that authoritative metadata comes from the blob body rather than Azure-side metadata.

## Pros and Cons of the Options

### Store each snapshot as a provider-owned frame with a fixed binary prelude, an uncompressed JSON header, and a payload segment

Use an outer frame consisting of magic bytes, frame version, reserved flags, header length, UTF-8 header JSON, and payload bytes.

- Good, because it separates provider framing from payload serialization.
- Good, because it supports fail-closed validation and additive header evolution.
- Neutral, because Azure metadata can still duplicate small diagnostics without becoming authoritative.
- Bad, because the provider must document and maintain the framing contract explicitly.

### Store the entire snapshot record as a plain JSON document

Represent metadata and payload in a single JSON document.

- Good, because the stored record is easy to inspect with basic tools.
- Good, because the outer format is simpler to describe.
- Bad, because binary payload bytes would need additional encoding or serializer-specific handling.
- Bad, because large payloads pay avoidable size and allocation overhead.

### Store compatibility metadata only in Azure blob metadata or blob index tags

Treat Azure metadata facilities as the authoritative persisted metadata surface.

- Good, because some metadata is visible in the portal and storage tools.
- Bad, because Azure metadata limits, permissions, and indexing behavior would become correctness dependencies.
- Bad, because restore behavior would no longer be portable or self-contained within the blob body.

## More Information

- [Architecture Decision Records](index.md)
