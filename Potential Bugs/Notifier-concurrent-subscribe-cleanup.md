# Disposing the last subscriber can detach a new concurrent subscriber

## Source location

- Project: `Inlet.Gateway.Abstractions`.
- Source file: [src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs lines 82-100](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs#L82-L100).
- Type: `InProcessProjectionNotifier`.
- Member: `Subscribe / RemoveSubscription`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Subscribe gets a shared subscriber collection and adds to it in a later step. Disposal removes the previous subscriber, checks whether the collection is empty, and removes its dictionary entry separately. A new subscriber can be added to a collection that has just been removed from the dictionary.

## Trigger

A shared notifier has one subscriber for a projection/entity pair. One thread disposes that subscriber while another thread subscribes to the same pair. For example: thread B obtains collection C at line 82; thread A removes the old subscriber at line 96, observes C empty at line 97 and removes C at line 99; thread B then adds its new subscriber to C at line 84 and returns successfully. An alternative ordering adds B after A's empty check and before dictionary removal.

## Potential impact

Subscribe can return success while later notifications cannot find its callback. The listener stays stale until it subscribes again. Cleanup of its detached handle can also reach a replacement collection at the same key.

## Evidence

- [src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs lines 72-100](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs#L72-L100): GetOrAdd and Add are separate; Remove, IsEmpty and key-only TryRemove are also separate.
- [src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs lines 49-67](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs#L49-L67), [src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs lines 162-181](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs#L162-L181), [src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs lines 192-199](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs#L192-L199), [src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs lines 227-234](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InProcessProjectionNotifier.cs#L227-L234): Notifications locate collections only through the dictionary. Each list access takes syncRoot independently; no shared lock protects membership in the dictionary.
- [src/Inlet.Gateway.Abstractions/InletInProcessRegistrations.cs lines 32-39](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/InletInProcessRegistrations.cs#L32-L39): The notifier is registered as a singleton, so multiple server-side subscribers share this instance.
- [src/Inlet.Gateway.Abstractions/IServerProjectionNotifier.cs lines 17-39](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Abstractions/IServerProjectionNotifier.cs#L17-L39): The public contract says notifications reach subscribers and Subscribe returns a handle that unsubscribes when disposed.
- [tests/Inlet.Gateway.Abstractions.L0Tests/InProcessProjectionNotifierTests.cs lines 134-168](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Inlet.Gateway.Abstractions.L0Tests/InProcessProjectionNotifierTests.cs#L134-L168), [tests/Inlet.Gateway.Abstractions.L0Tests/InProcessProjectionNotifierTests.cs lines 320-337](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Inlet.Gateway.Abstractions.L0Tests/InProcessProjectionNotifierTests.cs#L320-L337): Tests establish live subscribers receive callbacks and re-subscribing after cleanup remains functional, but these assertions are sequential.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Use a deterministic barrier or debugger pause between GetOrAdd and Add, or between IsEmpty and TryRemove, while disposing the previous handle; then notify the same pair and observe the newly returned subscription.
- No concurrency probe or repository test was executed during this review. The interleaving follows directly from source and the singleton registration.
- Do not treat callbacks already executing during Dispose as this defect; the concern is permanent loss of a successfully returned new subscription.

## Confidence

**High**. Both concrete interleavings remove a live subscription from the only notification lookup; the dictionary's thread safety does not make these multi-step operations atomic.
