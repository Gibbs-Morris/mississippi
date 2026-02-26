# Re-Review 04 — Technical Architect (Updated PLAN)

## Verdict
- Architecture is now explicitly cohesive: one auth contract across generated APIs, plus projection subscription extension at gateway boundary.
- Layering and dependency direction remain sound.

## Feedback
- **Issue:** `AuthorizationPolicyBuilder` usage is now explicit and correct for policy/roles/schemes combinations.
- **Why it matters:** Avoids partial auth semantics and runtime mismatch with existing generated endpoint behavior.
- **Proposed change:** No change required.
- **Evidence:** `PLAN.md` D7 and Phase 3 task 10.
- **Confidence:** High.

- **Issue:** Projection subscription auth remains at edge (hub), preserving transport-agnostic grains.
- **Why it matters:** Keeps Orleans domain model clean and avoids coupling to ASP.NET auth context.
- **Proposed change:** No change required.
- **Evidence:** `PLAN.md` principles and target flow; grain unchanged.
- **Confidence:** High.

- **Issue:** Future per-entity auth path is still non-goal but now has evolution route.
- **Why it matters:** Limits scope creep while preserving extensibility.
- **Proposed change:** No change required.
- **Evidence:** `PLAN.md` Known Limitations + Evolution (`ISubscriptionAuthorizationService`).
- **Confidence:** High.

## CoV
- Claims reviewed: architecture boundaries, policy semantics, extensibility.
- Evidence source A: updated `PLAN.md` architecture/decisions/evolution sections.
- Evidence source B: existing gateway/runtime separation and generator-convention model in repo.
- Confidence: High.