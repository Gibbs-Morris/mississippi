# Effect setup or cleanup failure stops later effects from running

## Source location

- Project: `DomainModeling.Runtime`.
- Source file: [src/DomainModeling.Runtime/RootEventEffect.cs lines 289-304](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/RootEventEffect.cs#L289-L304).
- Type: `RootEventEffect<TAggregate>`.
- Member: `EnumerateEffectSafelyAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The dispatcher catches ordinary errors only while advancing an effect's async enumerator. Calling HandleAsync, obtaining the enumerator, reading its current value, and disposing it sit outside that handler. An ordinary setup or cleanup failure therefore leaves the whole loop before later matching effects run.

## Trigger

Two matching effects are registered for one event. The first returns an async enumerable through an ordinary HandleAsync override that performs setup/validation and throws, or owns a custom asynchronous enumerator whose resource cleanup throws. The second effect should still be attempted under the existing error-isolation behavior.

## Potential impact

Later effects for an already saved event are skipped. The aggregate can log the overall failure and still return command success, so logs may be the only sign of the skipped work.

## Evidence

- [src/DomainModeling.Runtime/RootEventEffect.cs lines 249-268](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/RootEventEffect.cs#L249-L268): Effects run sequentially through EnumerateEffectSafelyAsync. An exception escaping that helper exits the foreach before remaining effects are invoked.
- [src/DomainModeling.Runtime/RootEventEffect.cs lines 289-307](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/RootEventEffect.cs#L289-L307): HandleAsync/GetAsyncEnumerator and Current occur in a try with only finally, and DisposeAsync is directly awaited in that finally.
- [src/DomainModeling.Runtime/RootEventEffect.cs lines 319-335](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/RootEventEffect.cs#L319-L335): The intended swallow/log/record-error behavior applies only to MoveNextAsync through TryMoveNextAsync.
- [src/DomainModeling.Runtime/RootEventEffect.cs lines 25-29](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/RootEventEffect.cs#L25-L29): The dispatcher describes all matching effects as invoked.
- [src/DomainModeling.Abstractions/EventEffectBase.cs lines 38-50](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Abstractions/EventEffectBase.cs#L38-L50): The public untyped HandleAsync wrapper directly calls the typed override, which returns IAsyncEnumerable; an ordinary method override can throw during this call rather than during MoveNextAsync.
- [src/DomainModeling.Abstractions/EventEffectBase.cs lines 66-72](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Abstractions/EventEffectBase.cs#L66-L72): The abstract override is not constrained to a compiler-generated async iterator. Returning a custom enumerable or performing synchronous setup is part of the accepted signature.
- [tests/DomainModeling.Runtime.L0Tests/RootEventEffectTests.cs lines 143-160](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/DomainModeling.Runtime.L0Tests/RootEventEffectTests.cs#L143-L160): The existing test explicitly expects a throwing effect not to prevent a later matching effect from running, establishing the intended error-isolation behavior.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 573-587](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L573-L587): The aggregate catches a whole dispatch failure and logs it; it does not restart the root dispatcher's loop to execute skipped matching effects.
- [src/DomainModeling.Runtime/GenericAggregateGrain.cs lines 626-641](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/GenericAggregateGrain.cs#L626-L641): The outer command path returns OperationResult.Ok after persisted effects dispatch completes through its catcher.

## Verification notes

Static inspection of complete assigned files and supporting source/framework contracts. No production/test files changed and no runtime reproduction or tests executed.

- Add a second tracking effect after an effect whose ordinary HandleAsync body throws before returning the enumerable; verify the second currently is not invoked.
- Independently use a custom async enumerator with successful enumeration and throwing DisposeAsync to test the cleanup boundary.
- Keep OutOfMemoryException, StackOverflowException and ThreadInterruptedException propagation outside this finding; they are intentionally excluded by the existing critical-exception policy.
- A typical async iterator throws its body exceptions during MoveNextAsync, which is already handled. The issue is the other valid lifecycle boundaries, not that existing test's usual iterator path.

## Confidence

**High**. The exception boundary is explicit, and the supported abstract method signature plus an existing continue-after-failure test establish the realistic trigger and intended behavior.
