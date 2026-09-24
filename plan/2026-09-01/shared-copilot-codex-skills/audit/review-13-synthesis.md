# Review 13: Deduplicated synthesis

## Must

| Decision | Required edit | Rationale/evidence |
| --- | --- | --- |
| Accept | Make #540 the hard-cutover owner for `.agents/skills` versus `.github/skills`; update all active consumers and fail on stale/unowned roots. | All reviews identified the direct contradiction in the current skill-builder, Rules Manager, and key-principles docs. |
| Accept | Publish a per-surface contract and Docusaurus support page for CLI, Codex, Copilot app, VS Code, VS Code Local, cloud agent, code review, Visual Studio, and JetBrains. | CLI results cannot prove native host discovery or reload behavior; user selected tiered validation. |
| Accept | Deliver an offline structural validator with a concrete PowerShell command, Pester tests, JSON diagnostics, stable codes, and `PASS`/`FAIL`/`BLOCKED`. | Builders need an executable contract; missing evidence must not pass. |
| Accept | Add default-deny capabilities/network, trust hierarchy, secret/PII redaction, draft-only publication, isolated live fixtures, and privileged gh-aw controls. | Security and platform reviews found high-confidence injection, SSRF, exfiltration, and privilege risks. |
| Accept | Add commit-anchored manifests, versioned matrix, content digests, ownership locks, idempotent operation IDs, atomic artifacts, and cutover/rollback state machine. | Distributed and data-integrity reviews identified mixed-source, lost-update, and retry duplication risks. |
| Accept | Preserve runtime event/serialization/storage invariants in a protected register and mechanically compare persisted/computed identities. | Event-sourcing review found policy-backed but unenforced storage identity and wire-contract gaps. |
| Accept | Define deterministic hard thresholds, bounded model sampling, lazy loading, context metrics, scan/review budgets, and hash-based reuse. | Performance reviews found unbounded validation/review cost and undefined metrics. |
| Accept | Make #545 go/revise/stop a real dependency condition that unlocks, invalidates, or defers later work. | Principal-engineer review and issue #545 require decision-gated sequencing. |
| Accept | Add required CI/path triggers, case-sensitive Linux validation, generated gh-aw checks, CODEOWNERS, and `+semver: skip` enforcement. | Platform/tooling reviews found no existing required gate or ownership wiring. |
| Accept | Add user catalogue/onboarding, explicit error-message templates, retained-agent picker metadata, and old-agent/new-skill mappings. | DX/adoption reviews found migration discoverability and fallback gaps. |

## Should

| Decision | Required edit | Rationale/evidence |
| --- | --- | --- |
| Accept | Use lightweight versus full authoring/evaluation tiers, with full live sampling at scheduled/final gates. | Reduces routine maintenance cost while preserving high-risk evidence. |
| Accept | Add host cold/warm/restart semantics and guidance-contract fingerprints to harness results. | Prevents rollback claims from ignoring already-running sessions. |
| Accept | Add explicit Orleans grain/non-grain fixtures and non-guarantee scenarios. | Broad current scopes could over-apply grain rules or imply global ordering/exactly-once. |
| Accept | Add third-party skill provenance, license, dependency, and rollback policy. | External skills/plugins/MCP are unverified inputs. |
| Accept | Publish residual pre-existing runtime findings without silently fixing them. | Keeps the guidance programme single-purpose. |

## Could

| Decision | Rationale |
| --- | --- |
| Accept | Add generated catalogue pages and trend dashboards after the core evidence contract is stable. |
| Accept | Add computed generic storage-name fixtures and registry collision checks as future hardening if runtime scope is explicitly opened. |
| Accept | Add a harmless sample skill for demos and onboarding. |

## Won't

| Decision | Rationale |
| --- | --- |
| Reject | Make Copilot CLI the only supported surface. | Conflicts with the user's decision and official multi-surface guidance. |
| Reject | Create one skill per instruction file or persona. | Conflicts with #532's outcome-oriented boundary and would preserve catalogue duplication. |
| Reject | Put mandatory invariants only in skills. | Activation is probabilistic; root/nested AGENTS and deterministic gates are required. |
| Reject | Run privileged live model evaluations on untrusted PR content. | Violates default-deny and the existing gh-aw threat boundary. |
| Reject | Fix unrelated runtime defects as part of guidance migration. | Violates single-responsibility; record residuals or open separate issues. |
| Reject | Reintroduce Cursor mirrors/synchronization. | #563 / PR #530 completed that decision. |

## Final conclusion

The revised plan is implementation-ready after the contract and validator
details above are represented in the sub-plans. All twelve persona reviews
converged on the same sequencing: resolve ownership and security first, prove
the pilot, then migrate and retire only with objective evidence.

## CoV

This synthesis deduplicates twelve independent persona reviews. Repeated
findings were merged by root cause; every Must decision has evidence in the
corresponding review file and the repository/issue sources.
