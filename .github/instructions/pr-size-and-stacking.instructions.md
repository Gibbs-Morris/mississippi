---
applyTo: '**'
---

# PR Size and Stacked Delivery

Governing thought: Deliver one complete, reviewable change per PR, aiming for 600 changed lines or fewer and building dependent work through verified GitHub stacks.

> Drift check: Check the [GitHub stacking documentation](https://docs.github.com/en/pull-requests/get-started/about-stacked-prs), the [gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md), and `gh stack <command> --help` before stack operations; preview behavior and flags can change.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For stack work, agents **MUST** read and use the installed [gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md) and [stack-design reference](https://github.com/github/gh-stack/blob/main/skills/gh-stack/references/stack-design.md), or those upstream versions when unavailable locally, under the complete global stack policy. Why: Native lifecycle and recovery rules remain required.
- For PR titles and bodies, contributors **MUST** follow [write-pull-request-description](../../.agents/skills/write-pull-request-description/SKILL.md) with [its local binding](../PULL_REQUEST_TEMPLATE.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All human contributors and agents planning, implementing, reviewing, or landing changes. This file is the canonical policy for PR size and stack progression; specialized workflows add their own acceptance criteria.

## Planning and Size Judgment

Apply [the complete size and planning policy](../agent-guidance/global-policy.md#traceability-and-delivery), including independent lockfile provenance and separately reported logical, excluded, and raw counts.

## Advancement Gate

Apply [the complete advancement gate](../agent-guidance/global-policy.md#traceability-and-delivery) to the current head and relevant base before the next dependent layer; a quiet poll or local marker is not approval.

## GitHub and gh-stack Workflow

Follow [the mandatory stack lifecycle policy](../agent-guidance/global-policy.md#traceability-and-delivery) and the installed gh-stack skill, including its stack-design reference and applicable supporting sections.

## References

- [Google: Small CLs](https://google.github.io/eng-practices/review/developer/small-cls.html)
- [Google: What to look for in a code review](https://google.github.io/eng-practices/review/reviewer/looking-for.html)
- [Google: Handling reviewer comments](https://google.github.io/eng-practices/review/developer/handling-comments.html)
- [GitLab Handbook: Iteration](https://handbook.gitlab.com/handbook/engineering/workflow/iteration/)
- [GitHub: Native stacks announcement](https://github.blog/changelog/2026-07-30-stacked-pull-requests-are-now-in-public-preview/)
- [GitHub: Stacking agent-generated changes](https://github.blog/engineering/turn-one-giant-ai-generated-pull-request-to-a-reviewable-stack/)
