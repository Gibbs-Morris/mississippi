# Writer sends cursor updates to a different channel from a custom factory

## Source location

- Project: `Brooks.Runtime`.
- Source file: [src/Brooks.Runtime/Writer/BrookWriterGrain.cs lines 135-141](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Writer/BrookWriterGrain.cs#L135-L141).
- Type: `BrookWriterGrain`.
- Member: `PublishCursorAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Cursor observers use the registered IStreamIdFactory to choose their update stream. The writer builds the default stream identity itself. A supported custom factory that selects another valid namespace therefore places the UX projection observer and publisher on different channels.

## Trigger

Register IStreamIdFactory before AddEventSourcing with Create(brookKey) returning StreamId.Create("custom-cursors", brookKey). Activate the UX projection cursor at N and append N+1 through BrookWriterGrain. That cursor subscribes to custom-cursors while the writer publishes to CursorUpdateStreamName.

## Potential impact

An active UX projection cursor can stay at its old position and keep returning old data or an absent entity. The default factory avoids the mismatch. Brooks has an additional implicit default subscription that can mask part of its own behaviour, but it does not update the separate UX cursor.

## Evidence

- [src/Brooks.Abstractions/Streaming/IStreamIdFactory.cs lines 6-18](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Abstractions/Streaming/IStreamIdFactory.cs#L6-L18): The public abstraction maps brook keys to Orleans stream identifiers without restricting a custom implementation to the default namespace.
- [src/Brooks.Runtime/BrooksRuntimeRegistrations.cs lines 61-63](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/BrooksRuntimeRegistrations.cs#L61-L63): `TryAddSingleton<IStreamIdFactory, StreamIdFactory>` preserves a pre-registered application implementation.
- [tests/Brooks.Runtime.L0Tests/BrooksRuntimeRegistrationsTests.cs lines 120-131](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Brooks.Runtime.L0Tests/BrooksRuntimeRegistrationsTests.cs#L120-L131): ExistingStreamFactoryIsPreserved explicitly checks that host customizations survive AddEventSourcing, establishing an accepted extension point.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 120-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L120-L124): The UX observer uses StreamIdFactory.Create(brookKey) and subscribes only to that returned identity.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 36-45](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L36-L45): The UX cursor is not decorated with an implicit-subscription attribute; a default-namespace implicit subscriber cannot stand in for its custom stream registration.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 89-93](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L89-L93): Normal UX cursor reads return the cached value without a storage refresh.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 160-169](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L160-L169): UX version reads obtain this cursor, not BrookCursorGrain's independently cached default cursor.
- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 140-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L140-L143): The Brooks observer also uses the injected factory when attaching its observer.
- [src/Brooks.Runtime/Writer/BrookWriterGrain.cs lines 135-141](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Writer/BrookWriterGrain.cs#L135-L141): The writer chooses the same configured provider but hardcodes StreamId.Create(CursorUpdateStreamName, primaryKey) instead of consulting the factory.
- [src/Brooks.Runtime/StreamIdFactory.cs lines 21-24](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/StreamIdFactory.cs#L21-L24): The default factory exactly matches the writer's hardcoded identity, which explains why default configuration is safe.

Counter-evidence and limits:

- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 24-28](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L24-L28): Brooks has an implicit default cursor-stream subscription. This can activate the Brooks grain for default publications independently of its factory-selected observer attachment and must not be overlooked.

## Verification notes

Source inspection, including the public extension point and its existing preservation test. No runtime reproduction was run.

- Reproduce with an active UxProjectionCursorGrain and a custom namespace, then compare the writer's published stream ID to the factory's subscribed stream ID. That reproduction does not depend on Brooks observer attachment semantics.
- For BrookCursorGrain alone, verify whether the selected Orleans runtime attaches the default implicit observer when OnActivateAsync subscribes to a different explicit namespace. Source does not attach that default observer, but implicit activation can read the committed position and partially mask the mismatch. Do not claim every Brooks-only configuration stays stale.
- Brooks implicit delivery cannot restore UxProjectionCursorGrain's observer: that type lacks the attribute and UxProjectionGrain.GetLatestVersionAsync reads its own cursor. High confidence is based on this unambiguous affected path.
- No malformed factory result is required. The example uses a valid namespace and the original brook key. The accepted custom-factory registration is verified by an existing repository test.

## Confidence

**High**. Publisher and UX observer select different valid stream identities under an explicitly preserved customization. Brooks' implicit default subscription does not cover the unannotated UX observer or its cached-reader API.
