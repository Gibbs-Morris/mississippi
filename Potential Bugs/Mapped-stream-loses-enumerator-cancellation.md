# Mapped async stream drops the consumer's cancellation request

## Source location

- Project: `Common.Abstractions`.
- Source file: [src/Common.Abstractions/Mapping/AsyncEnumerableMapper.cs lines 40-47](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Abstractions/Mapping/AsyncEnumerableMapper.cs#L40-L47).
- Type: `AsyncEnumerableMapper<TFrom,TTo>`.
- Member: `Map`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The mapper's output is an async iterator with no way to receive the token passed to GetAsyncEnumerator. Its input enumeration therefore receives the default token. A consumer using WithCancellation on the mapped stream cannot cancel input work that relies on this token.

## Trigger

A consuming application wraps a cancellable network/queue async stream with this mapper, starts enumeration with a consumer token, and cancels while the source MoveNextAsync is awaiting input. The underlying source relies on the token supplied when its enumerator is created.

## Potential impact

The consumer can stay blocked and keep underlying work alive after requesting cancellation. No active production consumer was found in the repository, so this is a library-consumer concern. A source that captures a separate token can still cancel through that token.

## Evidence

- [src/Common.Abstractions/Mapping/AsyncEnumerableMapper.cs lines 40-47](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Abstractions/Mapping/AsyncEnumerableMapper.cs#L40-L47): The async-iterator method exposes no token parameter and uses await foreach(input) without WithCancellation or an explicit input GetAsyncEnumerator token.
- [src/Common.Abstractions/Mapping/IAsyncEnumerableMapper.cs lines 6-12](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Abstractions/Mapping/IAsyncEnumerableMapper.cs#L6-L12): The existing abstraction maps IAsyncEnumerable input to IAsyncEnumerable output. The output already has the standard enumeration-token channel; the public IMapper method shape need not be extended to preserve it.
- [src/Common.Abstractions/Mapping/MappingRegistrations.cs lines 16-20](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Common.Abstractions/Mapping/MappingRegistrations.cs#L16-L20): The mapper is exposed as the implementation of the open generic IAsyncEnumerableMapper service, so library consumers can encounter this wrapping behavior through DI.
- [tests/Common.Abstractions.L0Tests/Mapping/AsyncEnumerableMapperTests.cs lines 69-78](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Common.Abstractions.L0Tests/Mapping/AsyncEnumerableMapperTests.cs#L69-L78): The existing normal mapping test supplies TestContext.Current.CancellationToken to ToListAsync on the mapped output, but its finite source does not use cancellation and cannot detect the dropped token.
- [Framework or protocol reference](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream): The compiler's EnumeratorCancellation attribute exposes GetAsyncEnumerator's token to an async-iterator body; WithCancellation then supplies that token to a consumed async stream.

## Verification notes

Source inspection and the C# async-stream token model. No runtime reproduction was run.

- Use a custom IAsyncEnumerable input that records its GetAsyncEnumerator token, then awaits Task.Delay(Timeout.Infinite, token) in MoveNextAsync. Compare direct versus mapped enumeration with the same cancelable consumer token.
- No direct current production consumer was found by searching src and samples for IAsyncEnumerableMapper or AsyncEnumerableMapper; declarations and registration were the only matches.
- The interface prose does not explicitly promise cancellation forwarding. This contract uncertainty is why confidence is Medium despite certain token loss; evaluate intended transparent-stream mapping semantics during triage.
- The finding is about an existing standard output enumeration token being dropped, rather than proposing a new public feature. A private cancellable iterator can preserve the current public Map(input) signature.
- A token captured directly by the source at source creation can still work. The concrete lost token is the mapped output consumer's token.

## Confidence

**Medium**. The compiler/token mechanics make the loss deterministic, but the mapper has no explicit cancellation promise and no active production consumer was found.
