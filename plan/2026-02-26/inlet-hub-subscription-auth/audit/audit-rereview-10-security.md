# Re-Review 10 — Security Engineer (Updated PLAN)

## Verdict
- Updated plan resolves prior critical security issue: no policy/path leakage in `HubException`.
- Security posture is materially improved while preserving existing generated API semantics.

## Feedback
- **Issue:** Generic denial message and server-side structured logging are now aligned with least-information principles.
- **Why it matters:** Prevents projection/path enumeration through error responses.
- **Proposed change:** No change required.
- **Evidence:** `PLAN.md` D2, target flow, observability note.
- **Confidence:** High.

- **Issue:** Cross-surface parity can increase impact of misconfigured policies.
- **Why it matters:** If a policy is wrong, behavior is consistently wrong across transports.
- **Proposed change:** Keep explicit migration guidance and strong parity regression tests as planned.
- **Evidence:** `PLAN.md` migration + generator parity scenarios + acceptance criteria.
- **Confidence:** High.

- **Issue:** Rate limiting and token-refresh/revocation remain out of scope.
- **Why it matters:** Valid residual risks, but documented.
- **Proposed change:** No change required for this plan; keep as future hardening.
- **Evidence:** `PLAN.md` known limitations/evolution.
- **Confidence:** High.

## CoV
- Claims reviewed: confidentiality of auth failures, residual risk management.
- Evidence source A: updated `PLAN.md` decisions/limitations/evolution.
- Evidence source B: existing auth mode + convention behavior and gateway boundary controls.
- Confidence: High.