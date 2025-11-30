<!-- Auto-generated suggested test cases for EventSourcing core project -->
# Mississippi EventSourcing – Suggested Test Cases

## Project Metadata

- Assembly: `Mississippi.EventSourcing`
- Focus: Orleans grains (writer, reader, slice, head) + factory + registrations + stream id.

## Global Invariants

- Grains respect Orleans threading model (no shared mutable static state mutated).
- Head position monotonic non-decreasing.
- Slice caching never returns events outside requested min/max.
- Stream IDs are deterministic from BrookKey.

---

### File: EventSourcingRegistrations.cs

#### Type: EventSourcingRegistrations (static)

Member: `IServiceCollection AddEventSourcing(this IServiceCollection)`
Test Cases:

| ID | Scenario | Given / When / Then | Priority | Type |
|----|----------|---------------------|----------|------|
| REG1 | Registers required singletons | empty services -> call -> resolve IBrookGrainFactory, IStreamIdFactory, options registered | H | Unit |

---

### File: EventSourcingOrleansStreamNames.cs

#### Type: EventSourcingOrleansStreamNames (static)

Test Cases:

| ID | Scenario | Given / When / Then | Priority | Type |
|----|----------|---------------------|----------|------|
| STRN1 | HeadUpdateStreamName constant valid | value non-empty == "StreamHeadUpdates" | M | Unit |

---

### File: StreamIdFactory.cs / IStreamIdFactory.cs

Member: `StreamId Create(BrookKey)`
Test Cases:

| ID | Scenario | G/W/T | Edge | Pri | Type |
|----|----------|-------|------|-----|------|
| SID1 | Deterministic creation | same key twice -> same StreamId | Determinism | H | Unit |
| SID2 | Distinct keys differ | two keys -> StreamIds not equal | Uniqueness | H | Unit |

---

### File: Factory/BrookGrainFactory.cs

#### Type: BrookGrainFactory

Members: `GetBrookWriterGrain`, `GetBrookReaderGrain`, `GetBrookSliceReaderGrain`, `GetBrookHeadGrain`
Test Cases:

| ID | Scenario | G/W/T | Edge | Pri | Type |
|----|----------|-------|------|-----|------|
| GF1 | Resolves writer grain | mock grainFactory -> expect GetGrain with IBrookWriterGrain(key) called | DI proxy | M | Unit |
| GF2 | Resolves reader grain | similar to GF1 | | M | Unit |
| GF3 | Resolves slice reader grain | range key | | M | Unit |
| GF4 | Resolves head grain | key | | M | Unit |
| GF5 | Null dependencies throw | pass null logger | ArgNullException Validation | L | Unit |

---

### File: Head/BrookHeadGrain.cs

#### Type: BrookHeadGrain

State: `TrackedHeadPosition`, token ordering, stream subscription.
Test Cases:

| ID | Scenario | Given / When / Then | Edge | Pri | Type |
|----|----------|---------------------|------|-----|------|
| HG1 | Initial head default | new grain -> GetLatestPositionAsync -> -1 | Default | H | Unit |
| HG2 | OnNext newer updates | start -1 -> OnNext(newPos=5) -> head=5 | Monotonic | H | Unit |
| HG3 | OnNext older ignored | head=5 -> OnNext(newPos=3) -> head stays 5 | Ordering | H | Unit |
| HG4 | Token ordering skip | token1 newer than token2 -> second ignored | Token | M | Unit |
| HG5 | GetLatestPositionConfirmed loads storage | storage higher than cache -> cache updated | Consistency | M | Unit |
| HG6 | DeactivateAsync triggers DeactivateOnIdle | instrumentation | Lifecycle | L | Unit |
| HG7 | OnErrorAsync deactivates | call OnErrorAsync -> DeactivateOnIdle invoked | Recovery | M | Unit |

---

### File: Head/BrookHeadMovedEvent.cs

#### Type: BrookHeadMovedEvent (record)

Test Cases:

| ID | Scenario | Given / When / Then | Priority | Type |
|----|----------|---------------------|----------|------|
| BHME1 | Property value stored | construct with pos -> NewPosition matches | M | Unit |
| BHME2 | Equality semantics | two with same pos equal; diff pos not equal | M | Unit |

---

### File: Reader/BrookReaderGrain.cs

#### Type: BrookReaderGrain

Focus: Slice calculation & enumeration.
Internal method: `GetSliceReads(start,end,sliceSize)` (static) – target directly.
Test Cases:

| ID | Scenario | G/W/T | Edge | Pri | Type |
|----|----------|-------|------|-----|------|
| BRG1 | Single slice exact | start=0 end=9 size=10 -> one slice (0..9) | Boundary | H | Unit |
| BRG2 | Two slices boundary | start=0 end=10 size=10 -> slices (0..9),(10..10) | Boundary | H | Unit |
| BRG3 | Mid-range partial | start=5 end=24 size=10 -> buckets (0:5..9),(1:10..19),(2:20..24) | Partitioning | H | Unit |
| BRG4 | Start>End throws | start=5 end=4 -> ArgException | Validation | H | Unit |
| BRG5 | Size zero invalid | sliceSize=0 -> ArgOutOfRange | Validation | H | Unit |
| BRG6 | ReadEventsAsync full end null | no readTo -> queries head grain once | Head fetch | M | Unit |
| BRG7 | ReadEventsBatchAsync aggregates | enumerates underlying stream | Aggregation | M | Unit |
| BRG8 | Cancellation respected | cancel during enumeration -> throws | Cancellation | M | Unit |

---

### File: Reader/BrookSliceReaderGrain.cs

#### Type: BrookSliceReaderGrain

Test Cases:

| ID | Scenario | G/W/T | Edge | Pri | Type |
|----|----------|-------|------|-----|------|
| BSR1 | Populates cache first read | empty cache -> ReadAsync -> calls storage | Cache | H | Unit |
| BSR2 | Does not refetch when full | cache full slice -> second ReadAsync no storage call | Cache | M | Unit |
| BSR3 | Filters below minReadFrom | cached events before start skipped | Range | M | Unit |
| BSR4 | Stops after maxReadTo | stops enumeration early | Range | M | Unit |
| BSR5 | ReadBatchAsync collects | returns immutable array size matches filtered | Batch | L | Unit |
| BSR6 | DeactivateAsync clears cache | after call cache empty | Lifecycle | L | Unit |

---

### File: Writer/BrookWriterGrain.cs

#### Type: BrookWriterGrain

Test Cases:

| ID | Scenario | G/W/T | Edge | Pri | Type |
|----|----------|-------|------|-----|------|
| BWG1 | AppendEventsAsync happy path | events-> storage append -> head event published | Basic | H | Unit |
| BWG2 | Expected head mismatch propagation | storage throws concurrency -> surface exception | Concurrency | H | Unit |
| BWG3 | Publishes BrookHeadMovedEvent | append success -> Stream.OnNext invoked | Event | H | Unit |
| BWG4 | Cancellation token passed | cancel before storage call -> operation canceled | Cancellation | M | Unit |

---

### Supporting Value Objects (Reader Options)

BrookReaderOptions / BrookProviderOptions

| ID | Scenario | Description | Pri | Type |
|----|----------|-------------|-----|------|
| OPT1 | Defaults | new options -> default values documented | L | Unit |

## Deferred / Open Questions

- REG2 (Adds memory streams) – Integration.
- REG3 (Combined registration) – Integration.
- HG8 (OnActivate subscribes stream) – Integration.
- Integration tests with in-memory Orleans test cluster for full event flow.
- Performance tests for large slice enumeration.
