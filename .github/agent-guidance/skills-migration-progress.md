# Skills migration progress

Checkpoint for the instruction-only [#532 goal](https://github.com/Gibbs-Morris/mississippi/issues/532).
This file is outside startup instructions. Verify live heads, rules, CI, and
reviews before resuming; a checkpoint is not an advancement gate.

## Scope and state

The September 26 user scope covers Copilot instructions, shared skills, and
final essential-guidance consolidation. Custom agents, including
`.github/agents/**`, are excluded. The original detached checkout's two dirty
sample lockfiles and untracked `.codex/` remain untouched. Work is isolated in
`.scratchpad/instruction-skills` in the current workspace.

Native stack #680 is the existing delivery chain; no duplicate skill or PR was
created. No merge or policy bypass is authorized by this goal.

| Order | PR | Capability | Current disposition |
| --- | --- | --- | --- |
| 1 | [#676](https://github.com/Gibbs-Morris/mississippi/pull/676) | Diagnose/repair failing quality checks | Resumed on current main; shorter candidate and new evidence in this layer. |
| 2 | [#679](https://github.com/Gibbs-Morris/mississippi/pull/679) | Capture validated reusable lessons | Pre-existing layer; refreshed review/scope audit waits for parent gate. |
| 3 | [#684](https://github.com/Gibbs-Morris/mississippi/pull/684) | Track GitHub work | Pre-existing layer; admission must distinguish bookkeeping from existing issue-delivery skill. |
| 4 | [#687](https://github.com/Gibbs-Morris/mississippi/pull/687) | Run/interpret mutation testing | Pre-existing draft; current-base checks and review evidence need refresh. |
| Last | Not created | Consolidate essential guidance in scoped AGENTS | Waits for all useful skill migrations and their gates. |

Main baseline: `30b306c076bd198329ae5c2f6e6e17fa3a0801e7`, 46 instruction files,
19 global instructions, six existing shared skills. Complete corpus and
representative task costs, including metadata, are in the
[build-repair audit](build-remediation-skill-migration.md#context-accounting).
Instruction maintenance conservatively selects all 46 bodies; it is deliberately
more expensive than ordinary scoped work. Native loading and repository-imposed
reads are measured and documented separately.

## Existing and remaining capability map

| Instruction sources | Destination or next assessment |
| --- | --- |
| Logging | Existing `add-dotnet-source-generated-logging`; retain local logging obligations. |
| PR descriptions | Existing `write-pull-request-description`; retain title/template/issue rules. |
| Post-push feedback | Existing `address-pull-request-feedback`; retain waits, isolated fixes, and gates. |
| ADR authoring | Existing `author-architecture-decision`; retain local lifecycle/metadata. |
| Product documentation | Existing `author-technical-documentation` and selected references; consolidate repeated page-focus guidance later. |
| Build failure remediation | Current `repair-build-failures` layer; six rules remain independent of activation. |
| Lesson capture, issue tracking, mutation | Resume the three existing upper layers rather than duplicate them. |
| General issue delivery | Existing `implement-github-issue`; not another instruction migration candidate. |
| Sample/framework explanations and examples | Mostly repository-specific knowledge; assess concise policy plus referenced documentation, not generic skill wrappers. |
| C#/DI/serialization/naming, projects, tests, global guards | Mostly mandatory scoped policy; assess coherent workflows only where they add decisions beyond existing skills. |
| Planning/Clean Squad instruction consumers | Audit references during final consolidation; preserve custom-agent consumers without migrating or editing those agents. |

These are audit dispositions, not a conclusion that no useful migration remains.
No next capability has been implemented while the first layer's gate is unmet.

## Verified limits and next action

- Codex CLI discovery plus four current-candidate synthetic trials passed. Copilot CLI discovery passed; model behavior failed before output on HTTP 402 additional usage limit. Resume the same fixtures after usage is available; do not count discovery as behavior conformance.
- Live main rules require code-owner approval. CODEOWNERS maps this layer to its author, `BenjaminLGibbs`; no independent required approval is present. Repository owners need to supply a valid independent approval path. This goal does not authorize changing protections to bypass it.
- Run/poll the current canonical pipeline and exact-head CI. Classify failed, skipped, missing, and historical results separately. Complete paginated feedback and description/issue freshness checks.
- After all first-layer requirements pass, audit #679's live comments and instruction-only scope, remediate its owning layer, validate both tools, and then apply the same gate. Older upper layers may include agent-shell edits from a broader historical scope; resolve that scope mismatch before claiming instruction-only delivery.
- Finish with a separate consolidation PR. Preserve all mandatory obligations and consumers; document compatibility stubs needed by unsupported Copilot surfaces and immutable custom-agent references. Test resolution and real surfaces before claiming parity or savings.

The complete objective remains unachieved until the full stack, remaining useful
capabilities, final consolidation, both-tool validation, and measured aggregate
context reduction are evidenced. Keep this record current at material milestones.
