---
applyTo: 'docs/Docusaurus/docs/**/*operation*.{md,mdx},docs/Docusaurus/docs/**/*ops*.{md,mdx}'
---

# Operations Documentation

Governing thought: Operations pages help engineers run Mississippi safely in production with explicit validation, telemetry, failure-mode, and rollback guidance.

> Drift check: Keep this file aligned with `docs/Docusaurus/docs/contributing/documentation-operations.md`.

## Rules (RFC 2119)

- This file **MUST** be applied only when the page is classified as `operations`. Why: Production guidance needs explicit safety and validation structure.
- Operations pages **MUST** explain when the guidance matters, what assumptions it relies on, how to validate it, what can fail, how to roll back, and what telemetry to watch. Why: Production changes need operational proof.
- Operations guidance **MUST** state what is safe live, what requires a maintenance window, and what can affect the whole cluster. Why: Blast radius matters.
- Operations pages **MUST** replace vague recommendations with concrete signals, thresholds, commands, dashboards, or decision criteria whenever evidence exists. Why: Generic advice is not operational guidance.
- Operations pages **MUST** cover the relevant subset of scaling, deployment order, mixed-version behavior, fault tolerance, disaster recovery, observability, secrets, security, and cost. Why: These are the dimensions production owners need.
- Operations pages **MUST NOT** hide constraints or pretend that tuning guidance exists when it has not been verified. Why: False confidence is dangerous in production.

## Scope and Audience

Contributors and agents authoring operations pages for Mississippi documentation.

## References

- Public guide: `docs/Docusaurus/docs/contributing/documentation-operations.md`
- General authoring: `.github/instructions/documentation-authoring.instructions.md`
