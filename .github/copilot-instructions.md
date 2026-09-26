---
applyTo: '**'
---

# Copilot Instructions

Governing thought: Copilot responses must follow repository guardrails—shared policies, SOLID verification, canonical build/test scripts, and CPM/versionless package management.

> Drift check: Build/test commands live in `./go.ps1` and `eng/src/agent-scripts/`; `Directory.Build.props`/`Directory.Packages.props` define MSBuild/CPM settings. Open them before advising.

## Rules (RFC 2119)

- Copilot **MUST** use [plain English](instructions/plain-english.instructions.md) when communicating with people, including conversations, reviews, and pull request comments or replies. Why: Readers should understand the message on first reading.
- Copilot **MUST** follow all applicable repository instruction files, especially shared guardrails, C#, naming, logging, and testing guidance. Why: Keeps suggestions compliant with their declared scopes.
- Copilot **MUST** follow [issue tracking and PR traceability](instructions/issue-tracking.instructions.md), including its intake timing, ongoing updates, and issue link on every PR. Why: Requested work needs a durable record through delivery.
- Copilot **MUST** use the [instruction-loading procedure](../AGENTS.md#instruction-loading), including its file-access or host-supplied fallback when shell discovery is unavailable, when selecting guidance not already supplied by the host. Why: All global and relevant requirements remain mandatory while unrelated instruction bodies stay out of startup context.
- For user-visible UX or browser-behavior changes, Copilot **MUST** follow [UX validation and PR evidence](instructions/ux-validation.instructions.md), including final Playwright screenshots and PR posting requirements. Why: Rendered behavior and reviewable visual evidence are required to validate UX.
- When a resolved problem reveals a failure mode or process gap that can recur across tasks, Copilot **MUST** follow [self-improvement learning](instructions/self-improvement.instructions.md). Why: Reusable learning should reach future Copilot and Codex tasks.
- Copilot **MUST** follow [token efficiency and reassessment](instructions/agent-efficiency.instructions.md), including during persistent goals. Why: Repeated effort needs new evidence or a better approach while preserving the full outcome and required gates.
- Copilot **MUST** follow the [mutation-testing policy](instructions/mutation-testing.instructions.md), prioritizing correct delivery and meaningful unit-test coverage over survivor chasing. Why: Mutation testing is an additional quality signal with no mandatory repository score threshold or ordinary completion gate.
- Copilot **MUST** follow [PR size and stacked delivery](instructions/pr-size-and-stacking.instructions.md), using the `gh-stack` skill for dependent PRs and completing each layer's CI/review gate before starting the next. Why: Reviewable increments prevent unchecked work from accumulating.
- Build/tidy guidance **MUST** use canonical scripts: `pwsh ./go.ps1` for full pipeline; `pwsh ./clean-up.ps1` to format/tidy; extra formatters **MUST NOT** be assumed. Why: Ensures consistent gates.
- For local iteration speed, Copilot **SHOULD** prefer `pwsh ./clean-up-targeted.ps1` with `-Files` or `-FileListPath` to clean only changed files, then **MUST** run full `pwsh ./clean-up.ps1` before completion/handoff. Why: Preserves canonical gates while reducing local feedback time.
- When build warnings include StyleCop/formatting issues (SA1137 indentation, SA1517 blank lines, SA1000 spacing, etc.), agents **MUST** run `pwsh ./clean-up.ps1` first rather than manually fixing formatting. Why: ReSharper CleanupCode applies `Directory.DotSettings` rules (expression bodies, brace placement, blank lines, wrapping, member ordering) consistently and fixes most formatting warnings automatically—manual fixes often introduce new violations or miss related issues.
- Package changes **MUST** use `dotnet add/remove package`; `Directory.Packages.props` **MUST** hold versions and project `PackageReference` items **MUST NOT** specify `Version`. Why: CPM compliance.
- After touching `.Abstractions`, Copilot **MUST** follow abstractions-project rules (create/use abstractions when triggers apply). Why: Maintains contract/implementation split.
- All generated/refactored code **MUST** match Microsoft C# conventions (file-scoped namespaces, expression bodies when beneficial, nullable guidance) and **MUST** verify SOLID after each change, fixing violations immediately. Why: Prevents design debt.
- Before answering usage/run questions, Copilot **MUST** consult `README.md` and treat it as authoritative for public APIs/env vars/examples. Why: Avoids drift.
- Copilot **SHOULD** prioritize public APIs from README when suggesting symbols and **SHOULD** respond concisely with file paths/lines when referencing code. Why: Improves traceability.
- When work spans many small fixes, Copilot **SHOULD** stage via `.scratchpad/tasks` per scratchpad rules and **MUST NOT** reference `.scratchpad/` from source/tests. Why: Enables safe coordination.

- Copilot **MUST** use [verify-change](../.agents/skills/verify-change/SKILL.md) when selecting, running, or assessing change-validation checks. Why: Required gates and evidence interpretation need one maintained procedure.

## Scope and Audience

These rules apply to Copilot chat/search responses for this repository.

## Change verification

Use [verify-change](../.agents/skills/verify-change/SKILL.md) with the
[local check bindings](agent-guidance/verify-change-bindings.md) for check selection,
execution, evidence reuse, and status assessment. If skill discovery is
unavailable or applicability is unclear, read both linked files directly.
The Rules above remain effective independently of skill activation; a
targeted or prerequisite check does not replace required completion gates.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- C#/naming/logging/testing: see respective instruction files under `.github/instructions/`
