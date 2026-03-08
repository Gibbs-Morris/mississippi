---
applyTo: 'docs/Docusaurus/docs/**/*troubleshoot*.{md,mdx}'
---

# Troubleshooting Documentation

Governing thought: Troubleshooting pages help the reader diagnose and resolve a Mississippi failure from evidence, not guesses.

> Drift check: Keep this file aligned with `docs/Docusaurus/docs/contributing/documentation-troubleshooting.md`.

## Rules (RFC 2119)

- This file **MUST** be applied only when the page is classified as `troubleshooting`. Why: Troubleshooting starts from symptoms, not subsystem tours.
- Troubleshooting pages **MUST** be organized by symptom and **MUST** include symptoms, meaning, probable causes, confirmation steps, resolution, verification, prevention, and related content. Why: Readers need an evidence-driven diagnostic path.
- Troubleshooting pages **MUST** use real error messages, real metrics, or real log signatures when available and **MUST NOT** fabricate stack traces. Why: Fake evidence misleads readers.
- Probable causes **MUST** explain how to confirm or rule each one out. Why: A cause list without confirmation steps is guesswork.
- Resolution steps **MUST** state whether the fix is safe live and whether restart, rollout, or state repair is required. Why: Recovery guidance has operational consequences.
- Troubleshooting pages **SHOULD** include concrete prevention guidance such as tests, alerts, rollout sequencing, or compatibility checks. Why: Good troubleshooting reduces repeat failures.

## Scope and Audience

Contributors and agents authoring troubleshooting pages for Mississippi documentation.

## References

- Public guide: `docs/Docusaurus/docs/contributing/documentation-troubleshooting.md`
- General authoring: `.github/instructions/documentation-authoring.instructions.md`
