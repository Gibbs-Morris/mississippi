# Projection cursor can miss an update while its live subscription starts

## Source location

- Project: `DomainModeling.Runtime`.
- Source file: [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 115-124](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L115-L124).
- Type: `UxProjectionCursorGrain`.
- Member: `OnActivateAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The cursor first reads position N from storage and then starts listening for live updates. An update processed between those steps can reach neither operation. Later position reads return the remembered value without checking storage again.

## Trigger

During first activation for a projection entity, the storage read observes N. An append commits N+1 and its notification is processed by the live stream before SubscribeAsync establishes this consumer. No further append occurs for that entity.

## Potential impact

Latest projection reads can keep using version N until another update or reactivation. If N was NotSet, a newly created entity can keep appearing absent. Its saved history remains intact.

## Evidence

- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 115-125](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L115-L125): The initial authoritative read occurs at 117 and the subscription is not created until 124; no second cursor read follows it.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 89-93](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L89-L93): All GetPositionAsync calls return trackedCursorPosition from memory, so later reads do not close the initialization gap.
- [src/DomainModeling.Runtime/UxProjectionCursorGrain.cs lines 158-176](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs#L158-L176): After activation the only normal position advancement is OnNextAsync receiving a later stream event.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 109-130](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L109-L130): Latest projection queries choose their historical snapshot using the cursor returned by GetLatestVersionAsync.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 160-169](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L160-L169): GetLatestVersionAsync directly requests this cached projection cursor rather than consulting storage.
- [src/Brooks.Runtime/StreamIdFactory.cs lines 21-24](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/StreamIdFactory.cs#L21-L24): The subscribed stream identity is the brook cursor update stream with the brook key.
- [src/Brooks.Runtime/Writer/BrookWriterGrain.cs lines 82-98](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Writer/BrookWriterGrain.cs#L82-L98): A writer commits storage and then publishes the resulting cursor, so another actor can advance storage while this activation is awaiting initialization.
- [src/Brooks.Runtime/Writer/BrookWriterGrain.cs lines 135-141](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Writer/BrookWriterGrain.cs#L135-L141): The publication uses the same cursor stream namespace and brook key as the subscribed stream.
- [samples/Crescent/Crescent.L2Tests/CrescentFixture.cs lines 193-195](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/samples/Crescent/Crescent.L2Tests/CrescentFixture.cs#L193-L195): A supplied host uses the default brook provider with AddMemoryStreams and PubSubStore; live cursor notifications are an actual supported configuration.
- [Framework or protocol reference](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-programming-apis#producing-and-consuming): The Orleans contract delivers publications to subscribed consumers. This call supplies no sequence token requesting earlier messages.

Counter-evidence and limits:

- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 24-28](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L24-L28): BrookCursorGrain is implicitly subscribed to CursorUpdateStreamName, unlike UxProjectionCursorGrain. Activation-triggered stream delivery can cover its read-before-observer-attachment ordering.
- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 136-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L136-L143): The Brooks implementation has a superficially similar read-then-SubscribeAsync sequence, but SubscribeAsync on its implicit stream attaches the existing logical observer rather than first creating the logical subscription.
- [src/Brooks.Runtime/Cursor/BrookCursorGrain.cs lines 91-112](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs#L91-L112): The cached Brooks read has a separate confirmed storage-read API that can reconcile newer storage positions; the UX cursor has no corresponding confirmed read.
- [Framework or protocol reference](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/streams-programming-apis#explicit-and-implicit-subscriptions): The Orleans implicit subscription model allows stream events to trigger activation and requires attaching processing logic rather than establishing a new logical subscription.

## Verification notes

Source inspection and the documented Orleans subscription model. No runtime reproduction was run. The missed-update timing needs confirmation with the selected stream provider.

- Use a controlled storage read and live stream to let N+1 pass the stream head before completing the new subscription, then repeatedly request the cursor without any later append.
- Provider scheduling can hide the race if a queued notification is processed after subscription. The trigger requires it to be processed before subscription; it does not claim every intervening publication is lost.
- Confirm the selected provider's default null-token starting point in a runtime reproduction. A provider that guarantees replay across this exact gap could reduce this trigger.
- The analogous Brooks cursor code has an ImplicitStreamSubscription attribute at BrookCursorGrain.cs:24. Its default stream is already implicitly subscribed and can trigger activation, so a Brooks lost-update trigger has not been established from ordering alone.

## Confidence

**Medium**. The read/subscription ordering and absence of subsequent reconciliation are certain. The missed notification requires the documented live-subscription timing and should be demonstrated with the configured provider.
