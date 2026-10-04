---
id: projection-fetches
title: Projection Fetch Outcomes
description: Reference the built-in HTTP projection fetcher's routes, missing-data outcomes, versions, and failures.
sidebar_position: 8
sidebar_label: Projection Fetch Outcomes
---

# Projection Fetch Outcomes

## Overview

`IProjectionFetcher` returns a nullable `ProjectionFetchResult`. For the built-in `AutoProjectionFetcher`, a null result, a `NotFound` result, and an exception have distinct causes.

## Applies To

- `Mississippi.Inlet.Client.ActionEffects.IProjectionFetcher`
- `AutoProjectionFetcher` and `ProjectionFetchResult`
- Latest and version-specific HTTP projection reads

## Routes And Version Requests

The [built-in fetcher](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs) resolves the projection type through `IProjectionDtoRegistry`. It rejects a null type and a null, empty, or whitespace entity ID. An unsupported type or null registered path returns null without making an HTTP request.

The default prefix is `/api/projections`. Latest reads use `{prefix}/{path}/{escapedEntityId}`; version-specific reads append `/at/{version}`. The entity ID is escaped with `Uri.EscapeDataString`. The supplied version is used without a range check.

The default URLs are relative, so the supplied `HttpClient` needs `BaseAddress`. Without one, `SendAsync` throws `InvalidOperationException` before a response exists. An absolute configured prefix can instead supply the request's origin.

The [interface's default `FetchAtVersionAsync`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/IProjectionFetcher.cs) delegates to `FetchAsync`. A custom fetcher that inherits this default performs its latest-read behavior even when a version is requested. The built-in fetcher overrides it with the version-specific route.

## HTTP Outcomes

Both built-in read methods share the same response handling:

- HTTP 404 returns the shared `ProjectionFetchResult.NotFound` sentinel before parsing the body.
- Other unsuccessful statuses throw through `EnsureSuccessStatusCode`.
- A successful response is deserialized using the requested projection type. JSON that deserializes to null returns null.
- Non-null data produces a result containing that object and the parsed response version.

A 404 describes the requested HTTP resource. The fetcher does not establish why it is missing or prove that an entity has no events.

The cancellation token is forwarded to the HTTP request and JSON read. HTTP, cancellation, and deserialization exceptions propagate; this fetcher does not add a retry loop.

## Result And ETag Values

[`ProjectionFetchResult`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/ProjectionFetchResult.cs) has public init-only `Data`, `Version`, and `IsNotFound` properties. `NotFound` has null data, version zero, and `IsNotFound` true.

The `Create` overloads reject null data, store the supplied data and version, and leave `IsNotFound` false. Reference-type data retains its object reference; the generic overload also permits value types and boxes them into `Data`. The overloads do not validate the version range. Direct initialization can set the properties independently.

For a successful built-in fetch, the version comes from parsing the ETag's tag after trimming double quotes. A missing or unparseable numeric tag yields zero. This is response metadata; the fetcher does not issue conditional requests or enforce a write-concurrency contract.

The built-in [projection controller](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection,TDto%7D.cs) sets an ETag for latest reads, but not for `GetAtVersionAsync`. With that version-specific endpoint, a successful request for version N therefore yields result `Version == 0`, unless the host adds the header separately. It is not confirmation of the requested version number.

The [result tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Inlet.Client.L0Tests/ActionEffects/ProjectionFetchResultTests.cs) cover both factory overloads, null guards, property initialization, and the shared sentinel. HTTP response handling above is verified from the fetcher implementation.

## Summary

Handle unsupported or null JSON results, explicit HTTP 404 results, successful data, and exceptions separately. A requested version also needs a fetcher that implements version-specific reads.

## Next Steps

- Read [Inlet Reference](./reference.md) for client composition and fetcher registration.
- Read [Read Models And Client Synchronization](../../concepts/read-models-and-client-sync.md) for the wider projection flow.
