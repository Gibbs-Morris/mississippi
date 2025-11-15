<!-- Auto-generated suggested test cases for EventSourcing.Abstractions -->
# Mississippi EventSourcing.Abstractions – Suggested Test Cases

## Project Metadata

- Assembly: `Mississippi.EventSourcing.Abstractions`
- Focus: Core value types & storage provider abstraction layer.

## Global Invariants & Cross-Cutting

- All value objects (`BrookKey`, `BrookRangeKey`, `BrookPosition`) enforce validation and provide implicit conversions that must be lossless and idempotent.
- Serialization attributes (`[GenerateSerializer]`, `[Alias]`, `[Id]`) – add one smoke test per type to ensure round-trip via Orleans serializer (integration).
- No method mutates input parameters (immutability of records/structs).

---

### File: BrookEvent.cs

Represents an immutable event payload.

#### Type: BrookEvent (record)

Purpose: Container for event metadata + bytes.

Member: `record BrookEvent { Data, DataContentType, Id, Source, Time, Type }`
Purpose: Property defaults & immutability.
Invariants:

- Defaults are empty (`ImmutableArray<byte>.Empty`, `string.Empty`, null `Time`).
- Setting custom values holds them.
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| BE1 | Default construction | new BrookEvent(); inspect props -> defaults | Defaults | M | Unit |
| BE2 | Custom initialization | construct with all props -> values preserved | Basic | H | Unit |
| BE3 | Large payload array | Data 1MB -> length preserved | Size | L | Unit |
| BE5 | Value equality | two identical instances -> Equals true | Equality | M | Unit |
| BE6 | Hash code stability | identical instances -> same hash; one field diff -> diff hash | Equality | M | Unit |

---

### File: BrookKey.cs

Composite key validation & conversion.

#### Type: BrookKey (readonly record struct)

Invariants:

- Rejects null type/id.
- Rejects separator char `|` in components.
- Combined length ≤ 1024.
- `ToString()` reversible.
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| BK1 | Valid construction | new("a","b") -> ToString == `a&#124;b` | Happy | H | Unit |
| BK2 | Null type | type null -> ctor throws ArgNull | Null | H | Unit |
| BK3 | Null id | id null -> ctor throws ArgNull | Null | H | Unit |
| BK4 | Embedded separator | type contains `&#124;` -> ctor ArgException | Validation | H | Unit |
| BK5 | Over max length | very long components -> ArgException | Bound | M | Unit |
| BK6 | FromString valid | FromString(a&#124;b) -> components correct | Parse | H | Unit |
| BK7 | FromString missing sep | "ab" -> FormatException | Parse | H | Unit |
| BK8 | Round-trip idempotent | key -> string -> key equals original | Idempotence | M | Unit |

---

### File: BrookPosition.cs

Represents position with sentinel -1 meaning not set.

#### Type: BrookPosition (readonly record struct)

Invariants:

- Negative (< -1) invalid.
- Default ctor sets Value = -1.
- Implicit conversions consistent.
- `IsNewerThan` uses strict `>`.
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| BP1 | Default not set | new BrookPosition() -> Value -1 & NotSet | Sentinel | H | Unit |
| BP2 | Valid positive | new(5) -> Value 5 NotSet false | Basic | H | Unit |
| BP3 | Invalid below -1 | new(-2) -> ArgOutOfRange | Bounds | H | Unit |
| BP4 | Implicit from long | (BrookPosition)7 -> Value 7 | Conversion | M | Unit |
| BP5 | ToLong symmetry | pos -> long -> FromLong -> same | Conversion | M | Unit |
| BP6 | IsNewerThan true | 5.IsNewerThan(3) -> true | Comparison | M | Unit |
| BP7 | IsNewerThan false | 3.IsNewerThan(3) -> false | Equality | M | Unit |

---

### File: BrookRangeKey.cs

Composite key with range semantics.

#### Type: BrookRangeKey (readonly record struct)

Invariants:

- Validates type/id (same as BrookKey).
- start >= 0, count >=0.
- String format: type|id|start|count.
- End = Start + Count - 1 (inclusive; -1 when Count == 0).
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| BRK1 | Valid construction | new("t","i",0,5) -> End 4 | Basic | H | Unit |
| BRK2 | Negative start | start -1 -> ArgOutOfRange | Bounds | H | Unit |
| BRK3 | Negative count | count -1 -> ArgOutOfRange | Bounds | H | Unit |
| BRK4 | Separator in type | type contains `&#124;` -> ArgException | Validation | M | Unit |
| BRK5 | Oversize composite | overly long -> ArgException | Length | L | Unit |
| BRK6 | FromString valid | FromString(`t&#124;i&#124;0&#124;10`) -> fields parsed | Parse | H | Unit |
| BRK7 | FromString bad parts | missing separators -> FormatException | Parse | H | Unit |
| BRK8 | FromString bad start | non-numeric -> FormatException | Parse | M | Unit |
| BRK9 | Round-trip symmetry | key -> string -> key equal | Idempotence | M | Unit |

---

### File: Attributes/EventNameAttribute.cs & BrookNameAttribute.cs

Attribute presence and retrieval.

#### Types: EventNameAttribute / BrookNameAttribute

Test Cases:

| ID | Scenario | Given / When / Then | Priority | Type |
|----|----------|---------------------|----------|------|
| ATTR1 | EventNameAttribute discovered | reflect sample annotated class -> attribute present | M | Unit |
| ATTR2 | BrookNameAttribute discovered | reflect sample annotated class -> attribute present | M | Unit |

---

### File: Storage/BrookStorageProviderHelpers.cs

Service registration extensions.

#### Type: BrookStorageProviderHelpers (static class)

Member: `RegisterBrookStorageProvider<TProvider>(IServiceCollection)`
Purpose: Registers reader & writer singletons.
Invariants:

- Adds exactly one IBrookStorageWriter & IBrookStorageReader service.
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| HLP1 | Registers provider | services -> call -> can resolve both interfaces | DI | M | Unit |

Member: `RegisterBrookStorageProvider<TProvider,TOptions>(IServiceCollection, Action<TOptions>)`
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| HLP2 | Registers + configure delegate | action sets property -> options bound | Options | M | Unit |

Member: `RegisterBrookStorageProvider<TProvider,TOptions>(IServiceCollection, IConfiguration)`

| ID | Scenario | Given / When / Then | Edge | Priority | Type |
|----|----------|---------------------|------|----------|------|
| HLP3 | Registers + config section | in-memory config -> options bound | Config | M | Unit |

---

### File: Storage/IBrookStorageReader.cs

Interface contract – create a contract test harness later.

Key Contract Test Ideas (not unit to this assembly):

- Reading beyond head yields empty stream.
- CancellationToken honored (cancellable enumerator).

---

### File: Storage/IBrookStorageWriter.cs / IBrookStorageProvider.cs

Contract invariants:

- Append returns new head >= previous head.
- Empty events list invalid.
- Concurrency via expectedVersion.

Deferred: Validate in provider implementation tests (Cosmos project).

## Deferred / Open Questions

- BE4 (Serializer round-trip) – Integration later with Orleans test cluster.
- Need explicit serialization round-trip tests? (Integration with Orleans test cluster.)
- Decide whether to add guard tests for attribute Id ordering.
