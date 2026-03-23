# Technical Perspective — Three Amigos

## First Principles: Technology Assessment

- The corrected plan is now using the right level of abstraction. It preserves the existing Tributary storage contract, keeps Blob-specific concerns internal, and does not broaden scope into a premature cross-provider redesign.
- The plan is explicit enough on the user-visible contract to guide implementation safely. The previous ambiguity around missing-read behavior is now closed because the two not-found cases name the caller-visible result directly.
- The remaining design choices stay aligned with the actual problem: larger payload persistence, self-describing storage, deterministic startup validation, and safer duplicate-version behavior than the current Cosmos upsert path.

## Feasibility Assessment

Yes. The corrected final plan is implementation-ready from a technical-lead perspective.

The plan now has the minimum precision needed on all blocking fronts:

- exact observable outcomes are explicit, including `Returns null` for missing exact-version reads and missing latest reads
- intentional divergence from Cosmos on duplicate-version writes is clearly documented as a product decision, not an accidental behavior drift
- serializer resolution, restart-survival proof, and unreadable-blob failure behavior are precise enough to drive deterministic tests
- large-payload validation is measurable rather than aspirational because the size matrix, evidence artifact, and buffering expectations are all stated

No additional planning rewrite is required before implementation starts.

## Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Implementation drifts from the explicit contract table during coding | Low | High | Treat the exact observable outcomes table as the primary behavioral acceptance source and map each row to tests. |
| Large-payload support is implemented correctly but with avoidable allocation overhead | Medium | High | Enforce the planned evidence artifact and explicit encode/decode pipeline review in the increment that introduces the frame and codec. |
| Duplicate-version conflict semantics surprise adopters expecting Cosmos overwrite behavior | Medium | Medium | Keep the product decision explicit in docs and tests, and verify diagnostics clearly identify conflict rather than transient failure. |
| Maintenance paths regress into payload-heavy enumeration | Medium | Medium | Preserve the plan requirement that prune and delete-all stay name-driven or header-light and prove that with focused tests. |

None of these are plan-level blockers. They are implementation controls.

## Architecture Constraints

- Existing patterns that must be respected:
  - Preserve the existing Tributary contract and Cosmos-like registration ergonomics.
  - Keep Blob provider concerns in a dedicated provider project rather than introducing new abstractions.
  - Keep serializer selection explicit and persisted through the stored frame rather than relying on ambient DI order.
- Dependency direction constraints:
  - Blob implementation depends on existing abstractions and Brooks serialization contracts.
  - No reverse dependency from abstractions back to the new provider.
- Integration points:
  - Tributary runtime snapshot persistence and restore behavior.
  - Brooks serialization registrations and serializer selection.
  - Azure Blob Storage client registration and hosted initialization.
  - Crescent Azurite-backed L2 coverage.

These constraints are already captured well enough in the final plan and no longer need clarification before implementation.

## Technology Choices

| Decision | Recommendation | Rationale | Alternative Considered |
|----------|---------------|-----------|----------------------|
| Contract behavior for missing reads | Keep explicit `Returns null` wording | Removes ambiguity and matches verified Cosmos caller behavior | Comparison-style wording; rejected because it leaves room for inconsistent tests and implementation |
| Duplicate-version behavior | Keep conflict-on-duplicate | Safer for Blob and explicitly intentional in the plan | Cosmos-style overwrite via upsert semantics; rejected for the new provider |
| Large-payload validation | Keep deterministic size matrix plus named evidence artifact | Makes the core value proposition reviewable and auditable | Informal confidence without recorded evidence; rejected |
| Maintenance-path design | Keep stream-local O(n) scans with payload-light behavior | Matches v1 scope while containing operational risk | Manifest or index redesign in v1; rejected as scope expansion |

## Complexity Estimate

- T-shirt size: M
- Justification: The planning ambiguities that previously risked rework are now resolved. What remains is contained provider implementation work with clear proof obligations.
- Key complexity drivers:
  - stored frame correctness and validation
  - serializer identity persistence and restart behavior
  - large-payload buffering discipline
  - stream-local naming and maintenance correctness

## Implementation Approach

1. Start from the final plan as written; no additional planning cycle is required.
2. Convert the exact observable outcomes table and risk-to-test matrix directly into L0 test cases before broad implementation spread.
3. Lock naming, conditional-write semantics, and frame/header format early because those choices drive compatibility and maintenance behavior.
4. Produce the large-payload evidence artifact in the same increment that introduces the frame and codec so allocation mistakes are caught early.
5. Keep the Crescent L2 scope narrow and trust-oriented, exactly as the plan specifies.

## CoV: Feasibility Verification

1. Claim about missing-read behavior: verified against [final-plan.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/final-plan.md) and [cosmos-behavior-evidence.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/cosmos-behavior-evidence.md). The final plan now explicitly states `Returns null` for both missing exact-version and missing latest-read scenarios, which matches the verified Cosmos caller behavior.
2. Claim about duplicate-version behavior: verified against [final-plan.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/final-plan.md) and [cosmos-behavior-evidence.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/cosmos-behavior-evidence.md). The plan intentionally diverges from Cosmos overwrite semantics and documents duplicate-version conflict behavior explicitly.
3. Claim about implementation readiness: verified against [final-plan.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/final-plan.md), [requirements-synthesis.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/requirements-synthesis.md), and [solution-design.md](c:/Users/benja/source/repos/batch/mississippi-2/mississippi/.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/solution-design.md). Contract outcomes, proof obligations, sequencing, and scope boundaries are now coherent and specific enough to implement.
4. Claim about remaining blockers: verified by re-reading the prior tech-lead concern and the corrected final plan. The only previous must-fix item was the missing explicit null-return wording, and that issue is now resolved.

## Conclusion

No must-fix items remain before implementation.

The corrected final plan is ready for implementation.
