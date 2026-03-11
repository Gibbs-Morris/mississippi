# Review 13: Synthesis

## Must
- Preserve the shared snapshot abstraction in V1 unless implementation evidence proves a gap.
- Keep the live Azure smoke path opt-in, configuration-driven, and free of in-repo secrets.
- Preserve deterministic pathing and snapshot/prune correctness in the provider core.

## Should
- Mirror Cosmos registration ergonomics and diagnostics shape.
- Keep keyed Blob clients plus hosted startup initialization explicit in the plan.
- Document both framework-host and sample-host wiring examples.
- Keep live-cloud smoke verification intentionally minimal-cost.

## Could
- Reassure reviewers that no new Orleans grain/event-contract slicing is introduced.
- Continue excluding any source-generator expansion from this epic.

## Won't
- No instruction extraction update: existing instructions already cover the required patterns.
- No abstraction redesign or migration tooling in this epic.

## Accepted changes applied
- The final PLAN explicitly calls out abstraction stability, keyed-service requirements, diagnostics parity, deterministic pathing, live-cloud secret handling, and both documentation example types.

## CoV
- **Key claims**: Review feedback was synthesized into explicit plan constraints without broadening scope.
- **Evidence**: `audit/audit-review-01-*` through `audit/audit-review-12-*` and the final `PLAN.md`.
- **Confidence**: High.
- **Impact**: The master plan is implementation-ready.
