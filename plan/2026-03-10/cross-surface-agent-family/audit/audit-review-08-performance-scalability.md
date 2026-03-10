# Review 08: Performance And Scalability Engineer

- Issue: The automation-enforcement specialist should explicitly look for stable performance regressions that can be guarded with benchmarks, perf tests, or architectural limits where practical. Why it matters: the user's automation rule covers any stable deterministic rule, which includes some repeatable perf checks. Proposed change: mention benchmark or performance-guard conversion under the automation specialist when a performance threshold is deterministic enough to encode. Evidence: user automation requirement plus repo benchmark guidance. Confidence: Medium.
- Issue: The build and review prompts should distinguish per-slice perf checks from branch-wide perf composition checks. Why it matters: some regressions emerge only after multiple slices land together. Proposed change: add branch-wide verification language for hotspots introduced by combined slices, especially in distributed or high-throughput paths. Evidence: inference from requested whole-branch verification loops. Confidence: Medium.

## CoV

- Claim: automation can cover some performance rules, but not all. Evidence: user explicitly distinguishes deterministic rules from ambiguous judgment. Confidence: High.