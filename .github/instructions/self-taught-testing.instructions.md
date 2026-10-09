---
applyTo: 'tests/**'
---

# Self-Taught Lessons: Testing

Governing thought: Test observable failure cases and keep test evidence accurate.

> Drift check: Before adding a lesson, compare overlapping instructions for conflicts and duplicates under `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Metric tests using `MeterListener` **MUST** make callback writes thread-safe and assertion reads snapshot-safe when parallel tests can emit the same meter. Valid approaches include a concurrent collection with snapshot-safe enumeration or a list whose every write and snapshot copy use the same lock, with assertions enumerating only that copy. Unique tags alone do not synchronize collection access. Why: The historical #815 full quality run threw `Collection was modified; enumeration operation may not execute` while `SnapshotStorageMetricsTests` enumerated a callback-fed `List`; issue #830 tracks the collector-safety correction.

- Shared startup tests **SHOULD** cover retry from failure callbacks. Why: In PR #909, joined-start coordination blocked `RetryFromFailureLogDoesNotPublishStaleDisconnected` until the failed attempt was cleared before its failure notification.

- Tests extending conditional matching **SHOULD** combine newly accepted conditions with missing representations. Why: PR #899 returned 304 for null projection state despite 100% controller line and branch coverage.

- Agents collecting test results **SHOULD** select the artifact paths reported by the run. Why: PR #899's collector parsed a JSON timestamp again, changed 9 October to 10 September, and included three historical RED reports.

## Scope and Audience

Agents writing or validating tests under `tests/**`.

## References

- Self-improvement governance: `.github/instructions/self-improvement.instructions.md`
- Test isolation: `.github/instructions/testing.instructions.md`
