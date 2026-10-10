---
applyTo: '**'
---

# Build Rules and Quality Gates

Governing thought: Every change ships only after a clean build, cleanup, and tests with zero warnings; mutation testing provides an additional quality signal.

> Drift check: Verify commands in `eng/src/agent-scripts/` (or `./go.ps1`) before use; scripts are the source of truth for switches and order.

## Rules (RFC 2119)

- Builds **MUST** finish with zero compiler/analyzer warnings; agents **MUST NOT** add `NoWarn`, relax severity, or suppress rules without explicit approval. Why: Zero-warnings is a hard gate.
- Agents **MUST** run and pass build, cleanup, and unit tests before calling work complete. Why: The required quality pipeline prevents regressions.
- For local iteration, agents **SHOULD** run `pwsh ./clean-up-targeted.ps1` against changed files to shorten feedback loops, but full `pwsh ./clean-up.ps1` **MUST** still pass before completion. Why: Faster inner loop without weakening gates.
- Agents **MUST NOT** add `[SuppressMessage]` or `#pragma warning disable` except for explicitly approved, minimal scopes. Why: Suppressions hide defects.
- Agents **MUST** keep StyleCop/ReSharper cleanup clean. Why: Consistent formatting enables readable diffs.
- Solution files **MUST** be edited in `.slnx` form only; `.sln` files **MUST NOT** be hand-edited because automation regenerates them with SlnGen during builds/cleanup for legacy tooling compatibility. Why: Prevents drift between canonical and generated solutions.
- Mississippi code changes **MUST** add comprehensive tests; Samples changes **SHOULD** add minimal illustrative tests. Why: Maintains coverage expectations per solution type.
- Agents **MUST** apply the [mutation-testing policy](mutation-testing.instructions.md) when choosing or interpreting mutation validation. Why: Mutation scores and tooling thresholds are additional signals, not mandatory repository completion criteria.
- Package versions **MUST** remain in `Directory.Packages.props`; project files **MUST NOT** add `Version` attributes. Why: Central Package Management avoids drift.

- Agents **MUST** use [verify-change](../../.agents/skills/verify-change/SKILL.md) when selecting, running, or assessing change-validation checks. Why: Required gates and evidence interpretation need one maintained procedure.

## Scope and Audience

All contributors changing Mississippi or Samples solutions.

## Change verification

Use [verify-change](../../.agents/skills/verify-change/SKILL.md) with the
[local check bindings](../agent-guidance/verify-change-bindings.md) for check selection,
execution, evidence reuse, and status assessment. If skill discovery is
unavailable or applicability is unclear, read both linked files directly.
The Rules above remain effective independently of skill activation; a
targeted or prerequisite check does not replace required completion gates.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Testing/mutation details: `.github/instructions/testing.instructions.md`
