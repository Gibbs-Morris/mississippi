---
applyTo: '**'
---

# Post-Push PR Review Polling

Governing thought: After pushing code to a branch with an open PR, agents sleep for human review, then systematically address every new comment one-at-a-time until none remain.

> Drift check: If GitHub MCP tools are unavailable, fall back to GitHub CLI (`gh`); confirm `gh` is installed with `gh --version` before use.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For the post-push feedback workflow, contributors **MUST** follow [address-pull-request-feedback](../../.agents/skills/address-pull-request-feedback/SKILL.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.
- When CLI fallback or pagination details are needed, agents **MUST** use [the exact GitHub thread-action reference](../../.agents/skills/address-pull-request-feedback/references/github-thread-actions.md). Why: Replies and resolution require the correct thread identities and actions.

## Scope and Audience

All agents that push code to branches associated with open pull requests.
