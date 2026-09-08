---
applyTo: '**'
---

# Agent Token Efficiency and Reassessment

Governing thought: Spend tokens on evidence and actions that advance the user's complete outcome, and reassess tactics when further effort stops producing useful information.

> Drift check: Keep this guidance aligned with [build remediation](build-issue-remediation.instructions.md), [scratchpad attempts](agent-scratchpad.instructions.md), and [PR review polling](pr-review-polling.instructions.md); their specific caps, waits, and gates still apply.

## Rules (RFC 2119)

- Agents **MUST** preserve the user's full acceptance criteria and applicable quality, safety, and review gates when optimizing token use. Why: A cheaper partial result is not completion.
- Agents **SHOULD** choose the next action by its expected contribution to an unresolved requirement, considering token cost, runtime, and tool overhead. Why: An unbounded goal is not a reason for unbounded repetition.
- Agents **MUST** honor explicit user budgets. Why: Spending limits are part of the task's constraints.
- Agents **MUST** report when the remaining budget cannot cover required work. Why: Budget exhaustion leaves work incomplete; it does not waive requirements.
- Agents **SHOULD** bound searches and tool output to the question being answered. Why: Large log dumps consume tokens without necessarily improving decisions.
- Agents **SHOULD** reuse still-current evidence. Why: Repeated context loading adds cost without resolving new uncertainty.
- Agents **SHOULD** batch independent reads when supported. Why: Combining independent reads can reduce tool overhead.
- Agents **SHOULD** stop optional research or validation once the relevant uncertainty is resolved. Why: Further checks need a new change, failure, missing fact, or unresolved concern to justify their cost.
- Agents **MUST** reassess before a third substantially equivalent attempt when two consecutive attempts produce the same failure or no useful new evidence. Why: This is an early checkpoint before existing retry caps, not permission to spend every allowed attempt.
- Agents **MUST** identify a changed hypothesis, input, method, or verified external condition before retrying after reassessment. Why: Rewording a command or plan does not make the same failed approach informative.
- Agents **SHOULD** keep a brief checkpoint at a stall or handoff: unmet requirement, attempts and evidence, current operation identifiers, and next decision. Why: Resuming work should not restart exhausted investigations.
- Agents **MUST** verify the state of an existing asynchronous operation before restarting it after an observation timeout. Why: A timeout or silent log does not prove that work stopped.
- Agents **SHOULD** use event-driven waits or bounded polling of a specific operation. Why: Waiting can be necessary without repeatedly loading unchanged output.
- Agents **MUST** honor the applicable workflow's required waiting intervals. Why: Choosing an efficient waiting strategy does not waive mandatory review time.
- Agents **SHOULD** continue authorized work using a materially different, evidence-backed approach after reassessment. Why: Stopping a failed tactic does not abandon the goal.
- Agents **MUST** explain the blocker when no safe, useful next action remains. Why: The user needs the evidence that prevents further progress.
- Agents **MUST** request only the missing input or decision needed to proceed when a blocker requires something the user can provide. Why: External conditions outside the user's control do not justify inventing a user decision.

## Scope and Audience

All repository agents, including long-running and persistent-goal workflows. Required instruction reads and workflow artifacts remain applicable. Use the host's goal-status rules when reporting completion or blockage.

## At-a-Glance Quick-Start

1. Name the unmet requirement and the evidence the next action should produce.
2. After repeated uninformative attempts, compare assumptions with current files, logs, tests, or authoritative documentation.
3. Choose a different approach, wait on verified ongoing work, or request the specific input needed to proceed.
4. Resume from the checkpoint; complete the original requirements and verify the current revision before claiming success.

## Examples

| Evidence | Next action |
|----------|-------------|
| Two runs return the same error with unchanged inputs | Inspect the cause and change the hypothesis or method before another run. |
| A test observation times out but its session is still running | Keep the session identifier and wait for that run; do not launch a duplicate. |
| Access is denied and the user can grant the needed access | State the inaccessible scope and request that access; do not interpret denial as an empty result. |
| A service outage blocks work and the user cannot restore it | Report the condition and what needs to recover; follow the host's waiting or blockage rules. |

## Core Principles

- Useful progress includes evidence that rules out an approach, even without a file change.
- Reassessment changes the next action; repeated plans and status summaries alone do not advance the task.
- Efficiency is judged across the whole task, including delegated work when used; shorter answers alone do not establish lower cost.

## Evidence and References

The two-attempt checkpoint is a Mississippi policy choice, not a measured optimum or a vendor limit. These sources inform the approach; no token-saving percentage is claimed.

- [OpenAI: Outcome-first prompts and stopping conditions](https://developers.openai.com/api/docs/guides/prompt-guidance?model=gpt-5.5#outcome-first-prompts-and-stopping-conditions): define success and missing-evidence behavior while keeping correctness ahead of loop reduction.
- [OpenAI: Testing and verification](https://developers.openai.com/api/docs/guides/latest-model#testing-and-verification): complete required checks and justify additional testing with changed evidence or remaining concerns.
- [First-principles thinking](../../docs/key-principles/first-principles-thinking.md) and [chain of verification](../../docs/key-principles/chain-of-verification.md): challenge assumptions and check conclusions against evidence.
