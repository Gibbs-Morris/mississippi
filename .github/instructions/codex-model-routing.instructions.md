---
applyTo: '**'
---

# Optional Codex Model Routing

Governing thought: Apply the literal Sol/Luna routing contract only to an explicitly selected, supported session.

> Drift check: Verify the [example settings](../../.codex/sol-luna.config.example.toml), [selection guide](../../.codex/README.md) and current client/account support before relying on this recipe.

All rules below apply only to a session whose operator explicitly selected the
supported Sol/Luna recipe. They grant no additional tool, publication or
delegation permission beyond the session's governing instructions.

## Rules (RFC 2119)

- The primary MUST use `gpt-6.1-sol` with `max` reasoning. Why: The selected recipe names literal primary settings.
- The primary MUST own planning, architecture, acceptance criteria, decomposition, difficult and security-sensitive decisions, integration and final review. Why: One owner keeps decisions coherent.
- The primary MAY make small integration corrections. Why: A handoff is unnecessary for a bounded integration correction.
- The primary SHOULD avoid unnecessary handoffs. Why: Coordination should improve the outcome.
- The primary MUST delegate well-specified implementation, refactoring, test-writing and supporting investigation to Luna workers, except small integration corrections. Why: Bounded workers execute work whose contract is already clear.
- Workers MUST use `gpt-6-luna` with `max` reasoning when supported. Why: Literal worker settings make delegation predictable.
- When spawn parameters support overrides, the primary MUST pass both model and reasoning explicitly. Why: Defaults alone do not establish the requested route.
- The primary MUST check selected role settings and effective routing. Why: Custom role settings can override explicit spawn settings.
- If requested routing is unsupported, the primary MUST report the limitation. Why: The missing capability needs a visible disposition.
- Unsupported routing MUST NOT trigger a model or reasoning substitution or an expensive fallback without user authorization. Why: A missing capability does not change the selected contract.
- Workers MUST retain their assigned scope and worker settings after reading root policy. Why: Policy reading does not expand an assignment.
- Workers MUST escalate ambiguity or blockers to the primary. Why: The primary owns material decisions.
- Workers MUST NOT spawn further agents without explicit primary authorization. Why: Further delegation needs a bounded assignment.
- Every delegated task MUST state its objective, context, acceptance criteria, file/module ownership and proportionate validation. Why: The worker needs a complete contract.
- Agents SHOULD prefer independent, non-overlapping writes. Why: Independent ownership reduces conflicts.
- Agents SHOULD agree shared interfaces before parallel edits. Why: Workers need the same contract before implementation.
- Agents MUST preserve other contributors' changes. Why: Shared work needs clear ownership.
- Workers MUST report changed files, actual commands/results and unresolved concerns. Why: Integration needs attributable evidence.
- The session MUST keep at most 16 concurrently open subagents, excluding the primary. Why: This is a session ceiling, not a target or a machine-wide claim.
- The primary MUST respect a lower host slot limit. Why: Configuration cannot create unavailable capacity.
- The primary SHOULD use the smallest useful worker set. Why: Extra workers need independently useful work.
- The primary MUST NOT split trivial work merely to occupy workers. Why: Trivial work gains little from coordination.
- The primary MUST NOT duplicate investigations unnecessarily. Why: Repetition consumes resources without independent progress.
- The primary MUST NOT dispatch tasks that require another worker's unfinished output. Why: Prerequisites need evidence before dispatch.
- Agents MUST coordinate expensive builds and full tests before running them. Why: Shared resources need explicit ownership.
- Workers MUST use targeted validation before integration. Why: A bounded change needs proportionate evidence.
- The primary MUST coordinate integration validation. Why: The assembled result needs evidence beyond each worker's checks.
- Agents MUST NOT write concurrently to shared mutable build outputs, test artifacts or database resources. Why: Conflicting writes can invalidate evidence or damage state.
- Agents MUST account for observable active sessions and resource pressure. Why: Capacity decisions need current evidence.
- Agents MUST NOT claim exclusive machine access or invent a machine-wide coordination mechanism. Why: Unobserved capacity is not an established guarantee.
- The primary MUST reduce active concurrency when contention, rate limits, duplicate work or review backlog undermines progress. Why: Completion includes downstream work.
- Concurrency changes MUST retain the selected model and reasoning settings. Why: Capacity relief does not authorize a different route.
- The primary MUST review the integrated result against acceptance criteria. Why: The coordinator owns the outcome.
- The primary MUST verify actual commands and results. Why: A worker report alone is insufficient proof.
- Worker claims MUST NOT be treated as proof. Why: Reported and verified results are different evidence states.
- Unverified items MUST be recorded. Why: Gaps need a visible disposition.
- Agents MUST prioritize the correctly reviewed outcome over worker count. Why: Worker count is not a success criterion.
- Agents MUST NOT alter ongoing goals while configuring routing. Why: A new profile must not disrupt active work.
- Agents MUST NOT alter existing workers while configuring routing. Why: Their current assignments and settings need to remain stable.

## Scope and Audience

Primary agents and workers in an explicitly selected, supported Sol/Luna
session. Reading this file does not select the profile or change other sessions.

## At-a-Glance Quick-Start

- Complete the support and account checks in the profile guide before selection.
- Verify effective primary and worker settings, including custom role overrides.
- Use the smallest useful worker set within the session ceiling and host limits.

## Core Principles

Literal routing and role ownership keep the selected contract consistent.
Configured ceilings, client acceptance and observed execution are separate facts.

## References

- [Profile selection and support checks](../../.codex/README.md)
- [Six literal example settings](../../.codex/sol-luna.config.example.toml)
- [RFC 2119 instruction structure](rfc2119.instructions.md)
