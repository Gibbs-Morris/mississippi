---
applyTo: 'tests/**'
---

# Self-Taught Lessons: Testing

Governing thought: Keep test observation safe when process-wide callbacks can run alongside assertions.

> Drift check: Before adding a lesson, compare overlapping instructions for conflicts and duplicates under `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Metric tests using `MeterListener` **MUST** collect callbacks in a concurrent collection with snapshot-safe enumeration when parallel tests can emit the same meter. Why: The #815 full quality run threw `Collection was modified; enumeration operation may not execute` while `SnapshotStorageMetricsTests` enumerated a callback-fed `List`.

## Scope and Audience

Metric tests under `tests/**` that observe process-wide callbacks.

## References

- Self-improvement governance: `.github/instructions/self-improvement.instructions.md`
- Test isolation: `.github/instructions/testing.instructions.md`
