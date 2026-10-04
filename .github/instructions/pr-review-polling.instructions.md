---
applyTo: '**'
---

# Post-Push PR Review Polling

Governing thought: After pushing code to a branch with an open PR, agents sleep for human review, then systematically address every new comment one-at-a-time until none remain.

> Drift check: If GitHub MCP tools are unavailable, fall back to GitHub CLI (`gh`); confirm `gh` is installed with `gh --version` before use.

## Rules (RFC 2119)

- After pushing code to a branch that already has an open PR, agents **MUST** sleep for 300 seconds (e.g. `Start-Sleep -Seconds 300` in PowerShell) before polling for new review comments. Why: Gives reviewers time to inspect the pushed changes before the agent acts.
- Local commits that have not been pushed **MUST NOT** start the polling loop. Why: Reviewers cannot comment on changes that are not yet visible on the PR.
- Agents **MUST** poll for unresolved PR review comments/threads using GitHub MCP tools by default; if MCP tools are unavailable and `gh` is installed, agents **MUST** fall back to GitHub CLI using `gh api` and `gh api graphql` for both polling and thread actions; if neither MCP nor `gh` is available, agents **MUST** stop and report the blocker. Why: MCP is the preferred integration; CLI is a reliable fallback when it can perform the exact workflow.
- When new unaddressed comments are found, agents **MUST** address them one-at-a-time in this exact order per comment: (1) read and understand the comment, (2) apply the minimal focused fix, (3) commit with a message scoped to that single comment, (4) push the branch, (5) reply to the comment thread with what was changed and the commit SHA, (6) resolve the thread. Why: Isolated commits make review history auditable and prevent batched regressions.
- Agents **MUST NOT** batch unrelated fixes into a single commit; each comment gets its own commit. Why: Keeps the fix traceable to the review feedback that prompted it.
- Agents **MUST NOT** resolve a thread before pushing the fix and replying with evidence. Why: Premature resolution hides unfinished work.
- If a comment is declined (disagree or out-of-scope), agents **MUST** reply with rationale and leave the thread open for the reviewer. Why: Only the reviewer or author should close a declined thread.
- Agents **MUST** inspect unresolved outdated threads for remaining concerns and record their disposition. Why: Moving code does not establish that feedback was addressed.
- Agents **MUST** satisfy the [advancement gate](pr-size-and-stacking.instructions.md#advancement-gate) before starting the next dependent PR. Why: Zero new comments or an exhausted polling cap does not prove CI success, approval, or resolution of existing feedback.
- Agents correcting a stacked PR **MUST** use the `gh-stack` skill to edit the owning layer and propagate changes before revalidating affected layers. Why: Fixes belong with the change reviewed, not in a later PR.
- If the exact thread reply or resolution action cannot be completed with MCP or `gh` on the current machine, agents **MUST** stop and report the blocker rather than substituting a top-level PR comment. Why: A top-level comment does not satisfy the required per-thread audit trail.
- After addressing all found comments, agents **MUST** sleep for another 300 seconds and poll again; this loop **MUST** repeat until either (a) a poll returns zero new unaddressed comments or (b) a configured maximum iteration cap is reached. Why: Reviewers may add follow-up comments after fixes land while still bounding the loop in adversarial scenarios.
- Agents **SHOULD** log each addressed thread (thread ID, status, commit SHA) in a running remediation ledger in their output. Why: Provides an auditable summary of all review actions taken.
- Agents **SHOULD** configure that maximum-iteration cap to a reasonable value (e.g., 20 iterations); if the cap condition in the previous rule is reached, agents **MUST** log the remaining unresolved threads in the ledger and stop with a summary for human review. Why: Prevents runaway loops in adversarial or high-volume review scenarios while keeping the stopping condition unambiguous.
- Agents **MUST** use the [address-pull-request-feedback skill](../../.agents/skills/address-pull-request-feedback/SKILL.md) for this feedback workflow. Why: One shared procedure keeps the retained review rules consistent across callers.

## Scope and Audience

All agents that push code to branches associated with open pull requests.

## At-a-Glance Quick-Start

Read the shared skill for collection, disposition, focused fixes, thread updates,
and polling. Apply the [Rules (RFC 2119)](#rules-rfc-2119) in this file, including
the 300-second wait, isolated commits, declined-thread handling, and advancement
gate. A quiet poll does not replace that gate.

## Procedure

Use the skill's [GitHub thread actions](../../.agents/skills/address-pull-request-feedback/references/github-thread-actions.md)
when CLI fallback or pagination details are needed. It covers inline comments,
review submissions, general discussion, thread identities, and exact reply and
resolution actions. Read the linked files directly if skill discovery is
unavailable; the retained rules remain mandatory.

## References

- PR review guide: `.github/instructions/pull-request-reviews.instructions.md`
- PR description authoring: `.github/instructions/pr-description.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Build issue remediation: `.github/instructions/build-issue-remediation.instructions.md`
