---
applyTo: '**'
---

# Build Issue Remediation Protocol

Governing thought: Fix each warning/error with the smallest safe edit in at most five focused attempts, deferring with context when blocked.

> Drift check: Open the build/cleanup/test scripts in `eng/src/agent-scripts/` before running them; scripts remain authoritative for switches and order.

## Rules (RFC 2119)

- A single issue (one warning/error at a specific location) **MUST** get no more than five focused fix attempts before deferral. Why: Prevents thrash.
- When deferring, agents **MUST** leave the code compiling/consistent and **MUST** create/update a `.scratchpad/tasks/...` item with `status=deferred`. Why: Keeps the tree stable and enables pickup.
- Agents **MUST NOT** broaden scope: fix only the lines required; large refactors are out of scope. Why: Minimizes regression risk.
- Agents **MUST NOT** relax analyzers, add `NoWarn`, edit generated code, or introduce `[SuppressMessage]`/`#pragma` without explicit approval. Why: Zero-warnings policy is non-negotiable.
- Project files **MUST NOT** add package versions; package changes **MUST** follow Central Package Management. Why: Avoids NU10xx noise and drift.
- Agents **MUST** obey `.editorconfig`/`Directory.Build.props` conventions while fixing. Why: Keeps formatting and settings consistent.

## Scope and Audience

Agents fixing build/analyzer/style issues in Mississippi and Samples solutions.

## Remediation workflow

Use [repair-build-failures](../../.agents/skills/repair-build-failures/SKILL.md)
for the observed-failure diagnosis, repair, and verification workflow described
by this adapter. If automatic skill discovery is unavailable, read the linked
skill directly. The six rules above remain effective whether or not the skill
is loaded.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Quality gates: `.github/instructions/build-rules.instructions.md`
