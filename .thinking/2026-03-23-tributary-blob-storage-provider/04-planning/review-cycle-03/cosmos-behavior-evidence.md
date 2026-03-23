# Tributary Cosmos Provider Behavior Evidence

**Date**: 2026-03-23  
**Scope**: Caller-visible behavior in the existing Tributary Cosmos snapshot provider  
**Thoroughness**: Medium

## Findings Summary

| Scenario | Caller behavior | Evidence |
| --- | --- | --- |
| Read missing exact version | Returns `null` | `SnapshotContainerOperations.ReadDocumentAsync()` catches Cosmos `404 NotFound` and returns `null`; this flows to the provider's nullable snapshot return. |
| Read latest missing | Returns `null` | Latest-read query yields no results and the repository returns `null`. |
| Delete missing | Idempotent and non-throwing | `DeleteDocumentAsync()` catches Cosmos `404 NotFound` and returns `false`; repository behavior is effectively silent for callers. |
| Duplicate-version write | Silent overwrite via upsert | Cosmos write path uses `UpsertItemAsync`, so an existing version is replaced rather than rejected. |
| Corrupt or unreadable stored data | Exception propagates | Mapping path does not add graceful validation fallback; deserialization or mapping failures surface to callers. |
| Startup configuration/initialization failure | Host startup fails | Initialization hosted service throws on incompatible container setup, producing deterministic startup failure. |

## Key Implication

The current architecture plan intentionally tightened duplicate-version behavior to fail rather than silently overwrite. That is probably the safer Blob behavior, but it is not the same caller-visible behavior as the current Cosmos provider. The plan therefore needs an explicit product decision on whether Blob must preserve current Cosmos-visible semantics or intentionally introduce stricter behavior in v1.