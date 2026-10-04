---
id: type-registries
title: Persisted Type Registries
description: Reference event and snapshot type lookup, registration collisions, and assembly-scan counts.
sidebar_position: 6
sidebar_label: Type Registries
---

# Persisted Type Registries

## Overview

Event and snapshot registries map persisted names to CLR types and types back to names. Their built-in implementations share the same lookup and collision behavior.

## Applies To

- `Mississippi.DomainModeling.Abstractions.IEventTypeRegistry`
- `Mississippi.DomainModeling.Abstractions.ISnapshotTypeRegistry`
- Their default implementations in `Mississippi.DomainModeling.Runtime`

## Lookup Contract

| Member | Behavior |
|--------|----------|
| `Register(name, type)` | Attempts to add both lookup directions without overwriting existing entries |
| `ResolveType(name)` | Returns the registered CLR type, or null for a valid unknown name |
| `ResolveName(type)` | Returns the registered name, or null for an unknown non-null type |
| `RegisteredTypes` | Exposes the live name-to-type dictionary through a read-only interface |
| `ScanAssembly(assembly)` | Processes types carrying the relevant storage-name attribute |

The [event](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/EventTypeRegistry.cs) and [snapshot](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/SnapshotTypeRegistry.cs) implementations compare names with `StringComparer.Ordinal`. Lookups are case-sensitive, and supplied names are not trimmed or normalized.

`RegisteredTypes` is a view of the current map, rather than an immutable copy captured when the property was read.

## Registration Collisions

For sequential registrations, the first type registered for a name stays associated with that name. Registering the same name for a different type does not replace it and does not add a reverse entry for that rejected registration.

Registering a second distinct name for an already registered type adds another name-to-type entry, but the type-to-name lookup retains its first name. For example, registering name `A` for a type and then name `B` for that same type allows both names to resolve to the type; resolving the type's name still returns `A`.

The two directions therefore need not be a one-to-one mapping. They are updated through separate dictionary additions, rather than one atomic paired update. A duplicate name is not reported as a registration exception.

Manual `Register` uses the supplied name. It does not derive or cross-check that name against the type's event or snapshot storage-name attribute; attribute-name extraction belongs to scanning and the higher-level registration helpers.

Snapshot registration does inspect `[SnapshotRetention]` before adding either mapping. Constructing that attribute with a nonpositive modulus throws `ArgumentOutOfRangeException`, so an invalid attributed type fails manual registration and assembly scanning before its mappings are added. The [snapshot registry tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/SnapshotTypeRegistryTests.cs) cover this retention failure.

## Assembly Scanning

The event registry looks for `[EventStorageName]`; the snapshot registry looks for `[SnapshotStorageName]`. Each calls `assembly.GetTypes()` and reads the attribute with inheritance disabled. Unattributed types are skipped.

The returned count increments for each attributed type processed after `Register` returns. It is not a count of newly added dictionary entries. Scanning an already registered assembly can therefore return a nonzero count without adding mappings.

Reflection or registration failures propagate. The scan does not provide a partial-type fallback for a failing `GetTypes()` call or roll back registrations already made before a later failure.

## Invalid Inputs

- Null names throw `ArgumentNullException`; empty or whitespace-only names throw `ArgumentException`.
- Null types passed to `Register` or `ResolveName` throw `ArgumentNullException`.
- A null assembly throws `ArgumentNullException`.

The existing [event registry tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/EventTypeRegistryTests.cs) and [snapshot registry tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/DomainModeling.Runtime.L0Tests/SnapshotTypeRegistryTests.cs) cover duplicate-name preservation, normal and unknown lookups, case sensitivity, guards, and empty scans.

## Summary

Registry names are ordinal and preserve existing mappings. Reverse lookup can retain one name while multiple stored names resolve to the same type, and scan counts describe processed attributed types rather than net additions.

## Next Steps

- Read [Spring's Aggregate Tutorial](../../samples/spring-sample/tutorials/building-an-aggregate.md) for event identity declarations in a complete example.
- Read [Tributary Cosmos Storage](../../tributary/storage-providers/cosmos.md) for snapshot identity and provider setup.
