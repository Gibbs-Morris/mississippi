# Plan Review Synthesis — Cycle 2

## Summary

- Total unique concerns: 11
- Must: 4 | Should: 5 | Could: 2 | Won't: 4
- Genuine conflicts requiring resolution: 0
- Positive observations: 6
- Final-cycle recommendation: light only after the Must items are incorporated; if any Must item remains open, the next cycle is still substantive.

## Action Items

### Must (blocking — plan cannot proceed without these)

| # | Concern | Raised By | Action Required |
|---|---------|-----------|-----------------|
| 1 | Observable contract outcomes are still implicit in parts of the parity matrix. | Tech Lead, QA Lead, DX, Developer Evangelist | Replace all comparison-style wording with exact observable outcomes for read-missing, delete-missing, duplicate-version write, corrupt or unreadable blob read, and initialization failures. The plan should say exactly what the provider returns or throws. |
| 2 | The large-payload promise is still not measurable enough to act as a gating claim. | Tech Lead, QA Lead, Performance | Add a deterministic payload-size matrix, explicit write/read buffering or copy-budget limits, and a named merge-evidence artifact with pass/fail criteria. Fold the encode/decode byte-movement expectations into this gate so large-payload viability is reviewable before coding starts. |
| 3 | The serializer contract is still not explicit enough in the plan itself. | Tech Lead, QA Lead, DX, Developer Evangelist | State the exact rule in the plan: zero matches for the configured serializer format fail startup, multiple matches fail startup, the persisted serializer identity is a concrete stable identifier, and unknown persisted serializer identities fail read deterministically with an actionable error. |
| 4 | Restart-safe proof for the non-default serializer path is still too optional. | QA Lead, Tech Lead, DX, Developer Evangelist | Make non-default serializer restart-survival mandatory in the merge evidence package. If the strongest proof cannot live in L2, require it explicitly at L0 and state why. |

### Should (important — significant improvement)

| # | Concern | Raised By | Action Required |
|---|---------|-----------|-----------------|
| 1 | The plan still does not lock one canonical registration path as an explicit acceptance point. | Tech Lead, DX, Developer Evangelist | Add a plan acceptance item that names the single blessed registration path and classifies any alternate overloads as advanced-only or deferred. |
| 2 | The mandatory Crescent L2 trust slice is still overloaded with too many assertions. | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | Narrow the mandatory L2 story to registration, write, restart, and read-back. Keep large-payload amplification proof, metadata inspection details, and other secondary advanced checks as additive evidence unless they are required elsewhere. |
| 3 | Observability and diagnostics proof is still under-specified. | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | Add an observability evidence matrix that names which failures must be proven via exception assertions, diagnostics, or both, and list the required triage fields for startup misconfiguration, duplicate-version conflicts, unreadable blobs, decode failures, and checksum failures. |
| 4 | Scan-path performance and maintenance-safety rules still need tighter wording. | QA Lead, Performance | Tighten the plan language so latest-read, prune, and delete-all are explicitly list-driven, page-by-page, and name-driven or header-light. State that non-selected candidates must never trigger payload download, decompression, or deserialization, and clarify that L0 is the mandatory proof layer while any L2 maintenance proof is additive. |
| 5 | First-time setup guidance is still not explicit enough about what is required versus optional. | DX, Developer Evangelist | In the plan's acceptance and documentation scope, classify required inputs, safe defaults, and advanced knobs such as initialization mode and `ListPageSizeHint`. |

### Could (nice to have — consider if time permits)

| # | Concern | Raised By | Action Required |
|---|---------|-----------|-----------------|
| 1 | Concurrency expectations beyond duplicate-version conflict are still unstated. | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | Add one short statement saying whether adjacent or near-simultaneous write semantics beyond duplicate-version conflict are part of the v1 contract or intentionally unspecified. |
| 2 | Quick-inspection support via optional Azure metadata duplication remains undecided. | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | Keep blob-body metadata authoritative. Only add Azure metadata duplication if it is demonstrably cheap and clearly non-authoritative; otherwise leave it out of v1. |

### Won't (acknowledged but out of scope)

| # | Concern | Raised By | Rationale for Deferral |
|---|---------|-----------|----------------------|
| 1 | Manifest, pointer, tag, or index-based lookup to avoid stream-local scans | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | All reviewers aligned that v1 can accept O(n) stream-local scans as long as scan paths stay lightweight and do not become payload-heavy. |
| 2 | Mixed-provider orchestration or a new shared storage abstraction | Tech Lead, QA Lead, DX, Developer Evangelist | Reviewers consistently supported keeping the launch scope to one Blob-backed provider behind the existing contract. |
| 3 | Real Azure scale or throughput certification as a release gate | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | Consensus is that v1 should prove functional trust plus bounded cost shape, not production-scale cloud benchmarking. |
| 4 | Compression or serializer feature expansion beyond the narrow v1 boundary | Tech Lead, QA Lead, DX, Performance, Developer Evangelist | Reviewers consistently preferred keeping `Off` and `Gzip` plus explicit serializer selection rather than broadening the feature surface before the first release is proven. |

## Conflicts Requiring Resolution

No genuine reviewer conflicts were found.

The feedback was highly convergent:

- The same four blockers were repeated across multiple reviews.
- Differences were mainly about emphasis, not direction.
- Performance-specific comments refined the large-payload and scan-cost requirements rather than contradicting other reviewers.

## Positive Observations

- The revised plan now tells a coherent product story: keep Tributary, swap storage, and handle materially larger snapshots without domain-code changes.
- Reviewers agreed the architecture is no longer the problem; the remaining work is about tightening execution and acceptance language.
- Scope discipline improved materially from cycle 1: no new shared abstractions, no mixed-provider orchestration, no index or manifest expansion, and no codec sprawl.
- The L0-first risk posture is much stronger, especially around naming, latest selection, duplicate-version safety, and corruption handling.
- Self-describing stored data remains a clear strength for restart trust, inspectability, and troubleshooting.
- The documentation scope is now pointed at the right adoption assets: canonical registration, setup translation, decision guidance, and metadata inspection.

## Final Validation Recommendation

The final validation cycle should be light, not substantive, but only after the four Must items above are folded into the plan.

Why this is a light-cycle recommendation once the Must items are addressed:

- No reviewer asked for an architectural change.
- No reviewer reopened the major scope or layering decisions from cycle 1.
- The remaining non-blocking feedback is mostly about sharpening acceptance criteria, proof placement, and documentation clarity.

Why it is still substantive if any Must item remains open:

- The unresolved Must items define the core public contract and the core product promise.
- Without them, the plan is still open to incompatible implementation and test interpretations.

## CoV: Synthesis Verification

1. Every action item traces to reviewer feedback: verified.
2. No reviewer concern was silently dropped: verified. Repeated concerns were deliberately merged into single actions where they described the same underlying issue.
3. MoSCoW categorization is justified: verified. Must items are the only ones that multiple reviewers treated as execution blockers or that directly affect the core contract and motivating promise; Should items improve delivery quality and reviewability without changing scope; Could items are clarifications or low-cost supportability choices; Won't items were explicitly and consistently deferred by reviewers.
4. Conflicts are genuine, not misunderstandings: verified. No material disagreement remained after deduplication; reviewers converged on the same blocking themes.
