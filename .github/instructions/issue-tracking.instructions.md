---
applyTo: '**'
---

# Issue Tracking and PR Traceability

Governing thought: Record planned repository work in a GitHub issue before implementation, keep the issue current, and link every PR to the work it delivers.

> Drift check: Keep this policy aligned with the [PR template](../PULL_REQUEST_TEMPLATE.md), [PR authoring](pr-description.instructions.md), and [PR advancement gate](pr-size-and-stacking.instructions.md#advancement-gate); verify [GitHub issue-linking behavior](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue) before changing linking guidance.

## Rules (RFC 2119)

- Before implementation, contributors **MUST** verify that the requested work has a relevant GitHub issue in this repository. Why: A chat request, external ticket, or branch name alone is not repository tracking.
- When no suitable open repository issue is linked, contributors **MUST** search repository issues for an existing issue covering the planned work before creating one. Why: Avoids duplicate tracking even when a supplied link is closed, unrelated, or external.
- Contributors **MUST** reuse a relevant open issue rather than create a duplicate. Why: Keeps decisions and progress together.
- If no suitable open issue exists, contributors **MUST** create a GitHub issue in this repository after planning and before implementation. Why: The issue records the intended work before changes begin.
- After planning and before implementation, contributors **MUST** document the problem, intended outcome, scope, acceptance criteria, implementation plan, and validation plan in the issue body or a clearly linked issue comment, using the private record described below for confidential details. Why: Existing and newly created issues need the same reviewable plan without forcing disclosure.
- Confidential reports and detailed plans **MUST** stay in a private security advisory, private vulnerability report, or restricted tracker until disclosure is authorized. Why: Issue tracking should not publish an unpatched vulnerability.
- Public issue and PR content **MUST** contain only disclosure-approved information, using a neutral, sanitized repository issue for confidential work. Why: Every PR can retain its issue link without exposing embargoed details or private-record URLs.
- The private record **MUST** identify the corresponding sanitized repository issue and hold the detailed plan and progress before implementation. Why: Authorized reviewers need full traceability even when the public record is deliberately limited.
- Contributors **MUST** keep the issue current at material milestones, scope or plan changes, blockers, PR creation or updates, and handoff or completion. Why: The issue is the durable task record.
- Issue updates **MUST** record completed and remaining work, relevant decisions or blockers, actual validation results, and PR links as those become available. Why: Future work starts from evidence rather than stale checklists.
- Contributors **MUST** preserve relevant existing issue content and discussion when updating the plan or status. Why: Progress updates should not erase the original request or others' decisions.
- Contributors **MUST** include the verified repository issue URL in saved implementation plans and builder handoffs. Why: Stateless or resumed builders need the durable tracking reference, not just prior chat context.
- Every PR **MUST** include at least one relevant issue number or URL from this repository in its description, including draft, automated, and stacked PRs. Why: Every proposed change needs traceability, regardless of author or base branch.
- For pre-existing PRs or unattended automated PRs whose producers cannot create issues before generating changes, maintainers **MUST** complete issue intake during triage before further implementation or review approval. Why: Generated changes can predate tracking, but still need a documented plan and issue link before acceptance.
- PRs **MUST** use a non-closing reference such as `Refs #123` for partial delivery or a stack layer that leaves issue scope unfinished. Why: An intermediate merge should not close the larger task.
- Closing keywords such as `Fixes #123` or `Closes #123` **MUST** be used only when the PR completes that issue's acceptance criteria. Why: Automatic closure should reflect completed work.
- Before declaring a PR ready to merge, contributors **MUST** verify that its issue references resolve to the intended issues and that issue status, remaining work, and validation match the current PR. Why: A stale or unrelated link does not establish traceability.
- If issue access or creation is blocked, contributors **MUST** report the blocker before implementation rather than invent an issue reference or silently omit tracking. Why: Unavailable tracking is an unmet prerequisite.

## Scope and Audience

All contributors and agents delivering repository changes, including code, tests, documentation, configuration, and automation. Read-only investigation and planning can precede issue creation; implementation begins with changes intended to deliver the planned outcome. Every PR is covered, including a planning-only PR.

The triage rule is an exception to pre-implementation timing for existing changes and unattended producers such as Dependabot or the scheduled guideline improver when their permissions defer issue creation until after execution. It is not an exception to issue content, updates, or PR links. During triage, a maintainer records the generated scope, acceptance criteria, validation, and any remaining implementation plan in a reused or new issue, then links the PR. Do not claim that this retrospective intake happened before generation. Interactive agent tasks use the normal plan-then-issue-then-implementation order.

An external tracker can provide context or hold confidential details, but the work still has a local repository issue. Optional child issues for an epic do not replace its required tracking issue. A relevant issue can cover several PRs when its plan and progress distinguish their outcomes.

For embargoed security work, use an existing neutral tracking issue or a sanitized issue whose title, scope, status, and validation reveal only approved information. Keep vulnerability details, exploit steps, sensitive acceptance criteria, and private-record links out of public issues, PR descriptions, and review replies. The private record supplies the full problem, plan, acceptance criteria, and validation to authorized reviewers; update the public issue with further detail only after disclosure is authorized. This changes where sensitive information is stored, not the issue-link requirement or planning order.

## At-a-Glance Quick-Start

1. Investigate the request and form a proportionate plan.
2. Verify the supplied issue, or search and reuse a relevant open issue; create one after planning if none fits.
3. Record the problem, scope, acceptance criteria, implementation steps, and validation in the issue before editing implementation files.
4. Implement the current outcome and update the issue when progress, decisions, or blockers change.
5. Link the issue in every PR description and add the PR link and status to the issue.
6. At handoff, record validation and remaining work accurately; a PR awaiting merge is not a merged change.

## Core Principles

- Plan first, record the issue, then implement.
- Reuse tracking and preserve context instead of creating parallel records.
- Issue updates capture meaningful changes rather than every tool invocation.
- Each PR explains its contribution without prematurely closing unfinished work.

## Linking Examples

| Delivery | PR description | Issue update |
|----------|----------------|--------------|
| A standalone PR completes the issue | `Fixes #123` | Link the PR and record validation and readiness. |
| A PR delivers part of a larger issue | `Refs #123` | Link the PR and identify completed and remaining acceptance criteria. |
| A stack layer targets its parent branch | `Refs #123` | Link each layer and record its scope and status. |

These numbers are placeholders; replace them with verified repository issues. A non-closing reference is an explicit issue link in the description, not an automatic-closing relationship. GitHub interprets closing keywords in PR descriptions only when the PR targets the default branch; recheck references after a base change and verify the issue's actual state after merge.

## References

- [PR description authoring](pr-description.instructions.md)
- [PR size and stacked delivery](pr-size-and-stacking.instructions.md)
- [Post-push review polling](pr-review-polling.instructions.md)
