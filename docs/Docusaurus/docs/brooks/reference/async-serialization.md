---
id: async-serialization
title: Brooks Asynchronous Serialization
description: Reference stream serialization methods, JSON buffering and cancellation, and service-registration boundaries.
sidebar_position: 10
sidebar_label: Asynchronous Serialization
---

# Brooks Asynchronous Serialization

## Overview

The asynchronous serialization interfaces read from or write to a `Stream`. The supplied JSON provider implements those interfaces alongside its synchronous memory-based methods.

## Applies To

- `Mississippi.Brooks.Serialization.Abstractions`
- `Mississippi.Brooks.Serialization.Json`

## Method Shapes

| Interface | Method and result |
|-----------|-------------------|
| `IAsyncSerializationReader` | `DeserializeAsync<T>(Stream, CancellationToken)` returns `ValueTask<T>` |
| `IAsyncSerializationWriter` | `SerializeAsync<T>(T, Stream, CancellationToken)` returns `ValueTask` |

Both methods have an optional token defaulting to `default`. Their [reader](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Abstractions/IAsyncSerializationReader.cs) and [writer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Abstractions/IAsyncSerializationWriter.cs) contracts expose generic methods; there is no asynchronous runtime-`Type` overload.

[`ISerializationProvider`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Abstractions/ISerializationProvider.cs) inherits both asynchronous interfaces and `ISerializationReader` and `ISerializationWriter`. The synchronous methods use `ReadOnlyMemory<byte>`; the reader also has a runtime-`Type` overload. The combined provider adds the `Format` property.

## JSON Stream Behavior

The [JSON provider](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Json/JsonSerializationProvider.cs) reports `System.Text.Json` as its format.

`DeserializeAsync<T>` calls `JsonSerializer.DeserializeAsync<T>` with the source stream and token. If deserialization produces null, it throws `InvalidOperationException`, matching the provider's synchronous null-result behavior.

`SerializeAsync<T>` checks that the destination is non-null, serializes the complete value to a UTF-8 byte array, and then awaits `destination.WriteAsync` with the token. The token is passed to the write; it does not cancel the preceding synchronous serialization step.

Asynchronous writing therefore still buffers the complete encoded payload. The method does not provide incremental encoding or a fixed memory limit. It does not call `Flush` or `FlushAsync`; the caller owns flushing and disposal of the stream. Neither asynchronous method disposes it.

Both methods use the stream's current position without seeking. Writing does not truncate existing content: replacing a longer payload can leave trailing bytes, while writing at the end appends another JSON value. Callers own positioning and, when replacing content in a seekable stream, truncation.

These serializer calls supply no `JsonSerializerOptions`. They use the platform defaults and applicable type attributes; this provider exposes no custom naming, converter, or other options callback.

Null source or destination streams throw `ArgumentNullException`. Malformed JSON can throw `JsonException`, and serializer or stream I/O failures propagate without provider retry. A failed or canceled write can leave partial JSON in the destination: the provider does not roll back bytes, reset the position, or truncate it. Before retrying, callers must discard the destination or restore its position and length as appropriate for that stream. A failed or canceled read can likewise consume source bytes; `DeserializeAsync` does not save or restore the source position. Before retrying, reposition a seekable source to its saved starting offset or supply a replacement source. Read failures do not provide an atomic, rewindable operation. A null decoded value instead uses the `InvalidOperationException` described above.

The [existing provider tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Serialization.Json.L0Tests/JsonSerializationProviderTests.cs) cover successful stream reads and writes, null deserialization results, format identity, and canceled operations.

## Service Resolution

Interface inheritance and dependency-injection registration are separate. The two registration helpers expose different service contracts:

| Helper | Registered services |
|--------|---------------------|
| `AddJsonSerialization()` | A singleton `ISerializationProvider` using the JSON implementation |
| `RegisterSerializationStorageProvider<TProvider>()` | Separate singleton mappings for the four reader/writer interfaces |

[`AddJsonSerialization`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Json/ServiceRegistration.cs) does not add individual inherited-interface aliases. Code resolving `ISerializationProvider` can call its inherited methods, but registering that interface alone does not register `IAsyncSerializationReader` as a separate service.

The [generic helper](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Serialization.Abstractions/SerializationStorageProviderExtensions.cs) does not register `ISerializationProvider`. Its four mappings each have their own singleton instance, rather than aliases to one shared instance. The [registration tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Serialization.Abstractions.L0Tests/SerializationStorageProviderExtensionsTests.cs) verify repeated resolutions are singleton within each interface.

`JsonSerializationProvider` is internal, so an external application cannot name it as that generic helper's type argument. If it needs the separate JSON reader/writer services, the host can register aliases that resolve its existing `ISerializationProvider` instance.

## Summary

Use the stream methods through the service contract actually registered by the host. The supplied JSON writer invokes and awaits the stream's asynchronous write API after buffering the entire payload, forwarding cancellation to that write. A completed `ValueTask` can make the await return without suspension; using this API does not guarantee yielding or offloading work to another thread. See [C# await behavior](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/await).

## Next Steps

- Read [Brooks Storage Providers](../storage-providers/index.md) for persistence contracts.
- Read [Runtime Composition](../../reference/runtime-composition.md) for host registration boundaries.
