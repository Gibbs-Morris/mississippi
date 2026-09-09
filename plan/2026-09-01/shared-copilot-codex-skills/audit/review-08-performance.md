# Review 08: Performance and Scalability Engineer

## Findings

- **High — Define context budgets and lazy loading.** Metadata-only catalogue
  discovery, applicable-scope loading, lazy bodies, and cold/warm budgets are
  required; byte estimates must be labelled proxies.
- **High — Bound scenario/scan work.** Use table-driven fixtures, timeouts,
  hash-based incremental scans, and scheduled exhaustive runs rather than
  repeating all work on every leaf.
- **High — Bound persona-review cost.** The full 12-persona requirement could
  scale to roughly 300 runs across the decomposition; cap concurrency and use
  risk-based quorums where methodology permits.
- **High — Separate deterministic and live model gates.** Bound calls, retries,
  output, time, and spend; keep structural/replay checks offline and live
  sampling scheduled/advisory.
- **High — Define collision metrics.** Record candidate count, selected skill,
  false positives/negatives, body tokens, p50/p95 cold/warm latency, and scan
  duration with explicit thresholds.
- **High — Give metrics units and denominators.** Use versioned schemas for
  bytes, normalized lines, actual/estimated tokens, files, cache hits, retries,
  and review calls.

## Strengths

Progressive disclosure, staged retirement, and activation/output separation
address the largest avoidable costs.

## CoV

Current instruction/agent byte scans, root loading rules, epic review
multiplication, and existing targeted/full cleanup guidance support the findings.
