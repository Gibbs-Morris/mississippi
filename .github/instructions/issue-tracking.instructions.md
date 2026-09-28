---
applyTo: '**'
---

# Issue Tracking and PR Traceability

Governing thought: Record planned repository work in a GitHub issue before implementation, keep the issue current, and link every PR to the work it delivers.

> Drift check: Keep this policy aligned with the [PR template](../PULL_REQUEST_TEMPLATE.md), [PR authoring](pr-description.instructions.md), and [PR advancement gate](pr-size-and-stacking.instructions.md#advancement-gate); verify [GitHub issue-linking behavior](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue) before changing linking guidance.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For issue intake, reconciliation, updates, or reference verification, contributors **MUST** follow [track-github-work](../../.agents/skills/track-github-work/SKILL.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All contributors and agents delivering repository changes, including code, tests,
documentation, configuration, and automation, are covered. Read-only
investigation and planning may precede issue creation; implementation uses the
normal plan-then-issue-then-implementation order. Every PR is covered,
including planning-only PRs.
