# Re-Review 02 — Solution Engineering (Updated PLAN)

## Verdict
- Updated plan now supports a coherent cross-surface auth story (aggregate/projection/saga HTTP + projection subscriptions).
- Adoption risk is low with explicit migration guidance.

## Feedback
- **Issue:** JWT-on-SignalR transport caveat is now documented; good.
- **Why it matters:** Prevents common 401 confusion during rollout.
- **Proposed change:** No change required.
- **Evidence:** `PLAN.md` migration step on `JwtBearerEvents.OnMessageReceived` for query-string token extraction.
- **Confidence:** High.

- **Issue:** Parity regression tests for generators are now explicitly included.
- **Why it matters:** Prevents accidental behavior drift for aggregates/sagas while adding subscription auth for projections.
- **Proposed change:** No change required.
- **Evidence:** `PLAN.md` Phase 5 and generator parity test scenarios.
- **Confidence:** High.

- **Issue:** L2 coverage is still intentionally follow-up (not blocking).
- **Why it matters:** End-to-end confidence is stronger with host-level auth pipeline validation.
- **Proposed change:** Keep as follow-up as planned; do not block implementation.
- **Evidence:** `PLAN.md` Evolution includes L2 integration test recommendation.
- **Confidence:** High.

## CoV
- Claims reviewed: rollout readiness, compatibility with existing consumer patterns, test depth.
- Evidence source A: `PLAN.md` migration/test/evolution sections.
- Evidence source B: existing test project layout under `tests/Inlet.Gateway.*` and `tests/Inlet.Gateway.Generators.L0Tests`.
- Confidence: High.