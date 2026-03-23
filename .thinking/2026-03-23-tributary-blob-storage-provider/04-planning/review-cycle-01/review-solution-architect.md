# Solution Architect Review Cycle 01

## Must-Fix

1. Re-sequence the implementation plan around the ADR dependency chain instead of the current coarse feature increments. The six decisions in [adr-notes.md](c:\Users\benja\source\repos\batch\mississippi-2\mississippi\.thinking\2026-03-23-tributary-blob-storage-provider\03-architecture\adr-notes.md) are not parallel details; they are dependency-ordered architecture constraints. Lock and verify them in this order before repository behavior is treated as stable: hashed naming, blob frame, serializer identity, payload-only compression plus checksum, conditional write plus maintenance semantics, then container initialization mode. The current plan allows repository and contract-integration work to advance before the persisted format and restart-safety boundaries are fully pinned down.

2. Add an explicit layering gate to Increment 1 that forbids Blob-specific details from leaking into Tributary contracts or shared abstractions. The plan currently says to mirror the Cosmos provider surface and later integrate serializer selection, but it does not say where those responsibilities must live. It should state that Blob naming, framing, compression, checksum validation, and Azure SDK operations stay internal to `Tributary.Runtime.Storage.Blob`; no new `*.Abstractions` package is introduced; and serializer resolution remains a composition-root concern rather than something pushed into repository or codec layers.

3. Define a concrete contract-parity matrix before implementation starts. "Preserve existing Tributary contracts" is too loose for an architecture-driving requirement. The plan should explicitly lock down the expected behavior for read-missing, delete-missing, duplicate-version writes, corrupt-frame reads, latest selection, prune scope, delete-all scope, and restart behavior when the ambient serializer configuration changes. Without that matrix, the team will end up designing against a slogan instead of a contract.

4. Move persisted-format verification into the same increments that introduce the format decisions. The plan currently defers strong L0 coverage to Increment 4, which is too late for architecture-significant invariants such as blob naming, frame versioning, serializer identity persistence, reserved-flag handling, and checksum failure behavior. Those tests are not test completion work; they are the executable form of the ADR boundaries and need to land with the corresponding implementation increment.

## Should-Fix

1. Tighten the phrase "mirror the Cosmos provider" so it clearly applies to adoption shape, not internal design. Matching DI, options, hosted initialization, and public registration is correct. Mirroring Cosmos internals is not. The plan should say that Blob internals remain Blob-native and allocation-aware, even when the public setup experience stays familiar.

2. Treat ADR authoring and acceptance as planned architecture work, not as optional follow-on documentation. Increment 1 currently mentions ADR or doc references only if project structure introduces visible setup changes. That is backwards. The architecture already identified six ADR-worthy decisions, and the plan should schedule their publication or at least their final acceptance checkpoint before implementation crosses those boundaries.

3. Remove "metadata visibility" from the core Crescent L2 exit criteria unless the team first decides that non-authoritative Azure metadata duplication is part of v1. The architecture keeps Azure metadata explicitly optional and non-authoritative. Making metadata visibility a required integration outcome risks turning an operational nice-to-have into a hidden architectural dependency.

4. Add an explicit dependency-direction checkpoint before project wiring is considered complete. The new provider should depend downward on Azure Blob infrastructure and existing Tributary contracts only. The plan should explicitly reject early extraction into `Common`, `Brooks`, or a new abstractions package unless a second consumer exists and the dependency direction remains clean.

5. Carry the accepted operational boundaries from the architecture into the plan's acceptance criteria. The plan should explicitly say that stream maintenance remains a stream-local linear scan, that Azurite does not validate production-scale Azure behavior, and that storage-account features such as soft delete or blob versioning change physical purge semantics. Those are architecture constraints, not documentation footnotes.

## Could-Fix

1. Add a small architecture checkpoint between Increment 2 and Increment 3 that confirms no pointer blob, manifest blob, tags-as-index, or latest-marker concept has slipped into the design. Those ideas are all valid future options, but none of them belong in this v1 plan.

2. Add an explicit observability completion item earlier than the final quality increment. Duplicate-version conflicts, decode failures, checksum failures, and list-cost visibility are part of the provider's operational architecture and should not be treated as late polish.

3. Add a short future-work note that preserves the option space for a later manifest or index design without biasing this implementation toward premature extension points.

## Won't-Fix For V1

1. Do not broaden this plan into a generic multi-provider storage abstraction redesign. The architecture explicitly rejected that move, and the plan should keep the feature scoped to one new provider.

2. Do not extract Blob naming, frame, or Azure SDK helper contracts into a shared package during initial delivery. That would introduce new layering surface without proof that the abstractions are stable.

3. Do not treat Azurite-based L2 tests as evidence for cloud-scale performance characteristics, retry parity, or operational behavior under real Azure account policies.

4. Do not add per-snapshot compression heuristics, additional codecs, or metadata-authoritative restore paths in v1.
