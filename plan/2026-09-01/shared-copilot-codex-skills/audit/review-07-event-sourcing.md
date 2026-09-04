# Review 07: Event Sourcing and CQRS Specialist

## Findings

- **Critical — Add a runtime-contract register.** Map wire attributes,
  storage identity, command/event boundaries, reducer/aggregate invariants,
  snapshots, registries, and retry guarantees to owners and tests.
- **Critical — Storage-name protection must be mechanical.** Compare computed
  event/snapshot names, aliases/member IDs, and deployed manifests; `+semver:
  skip` is valid only for guidance-only changes.
- **High — Define reducer/aggregate scenarios.** Cover no input mutation,
  new-instance replay, first-match dispatch, unmatched events, validation
  ordering, and error distinctions.
- **High — Define operation-level delivery guarantees.** Separate optimistic
  concurrency, at-least-once delivery, idempotent upsert, effects, and
  compensation; do not infer exactly-once from locks or retries.
- **High — Separate wire, persisted event, snapshot, and document contracts.**
  Do not apply Orleans requirements to every DTO or omit them at grain
  boundaries.
- **Medium — Computed generic/alias-derived names and registry collisions need
  future-proof tests.**

An existing snapshot reducer-hash mapping defect was observed during review;
it is pre-existing runtime scope and must be recorded as residual/unverified or
handled in a separately authorized issue, not silently fixed by this guidance
migration.

## Strengths

The plan correctly treats storage identity as separate from pre-1.0 API
compatibility and keeps runtime behavior outside skills.

## CoV

The review triangulated serialization/domain instructions with representative
runtime mapping, registry, retry, and snapshot code.
