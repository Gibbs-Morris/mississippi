---
applyTo: '**'
---

# Backwards Compatibility Policy

Governing thought: Pre-1.0 API changes are freely permitted subject to persisted-identity protection; agents avoid compatibility shims for patterns that only exist on the current branch.

> Drift check: Check `GitVersion.yml` (`next-version`) to confirm the current version bracket before applying compatibility rules.

## Rules (RFC 2119)

- **Pre-1.0 Freedom** - While the GitVersion `next-version` is below `1.0.0`, backwards compatibility **MUST NOT** be a constraint beyond the persisted-identity protection below; any public API, contract, event shape, serialization layout, or behavior **MAY** be changed freely subject to that protection and passing CI/CD pipelines (build + tests). Why: The framework is pre-release; shipping velocity and API quality outweigh stability guarantees.
- **Branch-Scoped Work Has No Legacy** - When working on a feature or chore branch, agents **MUST NOT** add backwards-compatibility shims, wrappers, adapters, or V2 types to preserve patterns introduced earlier on the same branch; the only contracts that matter are those present on `main`. If unsure, agents **MUST** compare the branch diff against `main` to determine whether the "old" pattern even exists in the merge target. Why: Agents have repeatedly added unnecessary compatibility code for patterns that will never exist in `main`, causing extra complexity and confusion.
- **Breaking Changes Must Update Consumers** - When a contract change on a branch breaks sample applications or tests, the same PR **MUST** update those consumers to compile and pass. Why: CI/CD must stay green; downstream breakage is not deferred.
- **Rolling-Update Compatibility (Post-1.0 Only)** - Once the repository reaches `1.0.0`, rolling-update compatibility rules apply: serialization member IDs and storage attribute names **MUST NOT** change without a versioned migration path; obsolete wrappers **SHOULD** be marked `[Obsolete]` and removed at the next major version at the latest. Why: Production deployments require orderly transitions, but compatibility shims must not linger.
- **Obsolete Wrappers Must Be Short-Lived** - Any wrapper, adapter, or compatibility shim introduced for a rolling update **MUST** be annotated with `[Obsolete("Remove in vX.0")]` and **SHOULD** be removed in the next planned major release. Why: Long-lived shims become invisible debt.
- **Event/Snapshot Storage Names Are Separate From API Compatibility** - `[EventStorageName]` and `[SnapshotStorageName]` attribute values **MUST NOT** change once data has been persisted to a real (non-test) store, regardless of version bracket. Why: Changing a storage name orphans previously written data.
- **Other Persisted Identities Are Separate From API Compatibility** - Persisted `[BrookName]` and Orleans `[Alias]` values, including derived constructed generic persistence identities, **MUST NOT** change during ordinary source/API refactors once data has been persisted to a real (non-test) store, regardless of version bracket. Why: A source rename can orphan data or break deserialization; API freedom does not authorize a data migration.

## Scope and Audience

All contributors and agents making code changes in this repository. This policy overrides default API-compatibility requirements while the repo is pre-1.0; its persisted-identity protection remains applicable.

## At-a-Glance Quick-Start

- **Pre-1.0?** Change APIs freely while preserving persisted identities. Fix the samples and tests in the same PR.
- **On a branch?** Only preserve contracts that exist on `main`, never patterns you introduced earlier in the same branch.
- **Adding a wrapper for compat?** Mark it `[Obsolete]` immediately and plan removal.
- **Renaming a persisted type?** Preserve its brook/storage names, Orleans alias, and derived persistence identities; a source refactor does not authorize data migration.

## Core Principles

- Ship quality over ceremony: pre-release means iterate fast.
- Branch work is ephemeral; only `main` defines the compatibility baseline.
- Compatibility shims are technical debt; minimize their lifetime.
- Persisted identity remains stable even when pre-release API compatibility is not required.

## References

- [Metadata and host composition](metadata-and-composition.instructions.md): proposed responsibility rules within their stated activation scope.
- GitVersion config: `GitVersion.yml` (`next-version` field)
- Orleans serialization: `.github/instructions/orleans-serialization.instructions.md`
- Storage naming: `.github/instructions/storage-type-naming.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
