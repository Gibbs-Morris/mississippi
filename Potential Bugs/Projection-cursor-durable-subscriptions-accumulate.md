# Projection cursor adds subscriptions on reactivation without releasing them

## Source location

- Project: `DomainModeling.Runtime`.
- Source file: [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 120-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L120-L124).
- Type: `UxProjectionCursorGrain`.
- Member: `OnActivateAsync / DeactivateAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Every activation creates a new explicit stream subscription and discards its handle. Orleans keeps such subscriptions until they are unsubscribed, even when the grain deactivates. The cursor neither releases them nor resumes an existing one. Its public DeactivateAsync contract also promises to unsubscribe.

## Trigger

A projection cursor is read, deactivates after idle collection or an explicit DeactivateAsync call, and is later read or activated by a stream notification. Repeat that normal lifecycle on an entity with cursor traffic.

## Potential impact

Repeated activation can leave growing subscription records. Updates can continue targeting a consumer that was told to deactivate, and earlier subscriptions lack a restored observer. Extra delivery work or errors depend on the provider. Duplicate callbacks are not claimed as inevitable.

## Evidence

- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 36-45](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L36-L45): The type implements IAsyncObserver and IGrainBase but has no ImplicitStreamSubscription attribute. The complete field inventory has no subscription handle.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 120-125](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L120-L125): A new explicit SubscribeAsync call occurs on every activation, and its handle is neither stored nor resumed.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 82-86](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L82-L86): DeactivateAsync schedules idle deactivation and performs no unsubscription.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 143-149](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L143-L149): OnErrorAsync also deactivates without removing or preserving the subscription.
- [src/DomainModeling.Abstractions/IUxProjectionCursorGrain.cs lines 28-33](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Abstractions/IUxProjectionCursorGrain.cs#L28-L33): The public method contract explicitly describes DeactivateAsync as releasing resources and unsubscribing from streams.
- [samples/Crescent/Crescent.L2Tests/CrescentFixture.cs lines 193-195](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/samples/Crescent/Crescent.L2Tests/CrescentFixture.cs#L193-L195): A supplied host configures explicit stream pub/sub storage via the Orleans PubSubStore convention.
- [Framework or protocol reference](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-programming-apis#recovering-from-failures): Explicit subscriptions survive deactivation until UnsubscribeAsync. Reactivated consumers should obtain existing handles and ResumeAsync; a new SubscribeAsync creates an additional subscription.

Counter-evidence and limits:

- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 24-28](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L24-L28): The default Brooks cursor stream has an implicit-subscription attribute, so it does not share the UX cursor's explicit logical-subscription lifetime.
- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 139-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L139-L143): On that implicit stream SubscribeAsync reattaches processing logic; discarding this handle is not evidence of subscription multiplicity.
- [Framework or protocol reference](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-programming-apis#writing-subscription-logic): An implicit subscription has one logical subscription per stream namespace, supports neither subscription multiplicity nor unsubscription, and needs no ResumeAsync. This differs from the UX type's unannotated explicit subscription.

## Verification notes

Static inspection of complete assigned files and supporting source/framework contracts. No production/test files changed and no runtime reproduction or tests executed.

- Count GetAllSubscriptionHandles across an activate/deactivate/reactivate cycle and confirm that the count increases.
- Verify that explicit DeactivateAsync removes subscriptions only after a future fix; current source contains no such operation.
- Do not infer implicit semantics from the class/interface prose: there is no implicit-subscription attribute in the compiled source.
- BrookCursorGrain.cs:24 has an implicit-subscription attribute for the default cursor stream. Its discarded SubscribeAsync handle at :143 does not establish explicit subscription accumulation; do not extend this finding to that default path.

## Confidence

**High**. The full type has an explicit new subscription per activation and no release/resume path; the framework's durable subscription lifetime is documented.
