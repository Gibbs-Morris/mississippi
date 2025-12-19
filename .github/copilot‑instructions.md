---
applyTo: '**'
---

# Copilot Instructions

Governing thought: Copilot responses must follow repository guardrails—shared policies, SOLID verification, canonical build/test scripts, and CPM/versionless package management.

> Drift check: Build/test commands live in `./go.ps1` and `eng/src/agent-scripts/`; `Directory.Build.props`/`Directory.Packages.props` define MSBuild/CPM settings. Open them before advising.

## Rules (RFC 2119)

- Copilot **MUST** follow all repository instruction files, especially shared guardrails, C#, naming, logging, and testing guidance. Why: Keeps suggestions compliant.
- Build/tidy guidance **MUST** use canonical scripts: `pwsh ./go.ps1` for full pipeline; `pwsh ./clean-up.ps1` to format/tidy; extra formatters **MUST NOT** be assumed. Why: Ensures consistent gates.
- Package changes **MUST** use `dotnet add/remove package`; `Directory.Packages.props` **MUST** hold versions and project `PackageReference` items **MUST NOT** specify `Version`. Why: CPM compliance.
- After touching `.Abstractions`, Copilot **MUST** follow abstractions-project rules (create/use abstractions when triggers apply). Why: Maintains contract/implementation split.
- All generated/refactored code **MUST** match Microsoft C# conventions (file-scoped namespaces, expression bodies when beneficial, nullable guidance) and **MUST** verify SOLID after each change, fixing violations immediately. Why: Prevents design debt.
- Before answering usage/run questions, Copilot **MUST** consult `README.md` and treat it as authoritative for public APIs/env vars/examples. Why: Avoids drift.
- Copilot **SHOULD** prioritize public APIs from README when suggesting symbols and **SHOULD** respond concisely with file paths/lines when referencing code. Why: Improves traceability.
- When work spans many small fixes, Copilot **SHOULD** stage via `.scratchpad/tasks` per scratchpad rules and **MUST NOT** reference `.scratchpad/` from source/tests. Why: Enables safe coordination.

## Scope and Audience

These rules apply to Copilot chat/search responses for this repository.

## At-a-Glance Quick-Start

- Use shared guardrails and C#/naming/logging/testing instructions as the baseline.
- Build/test with `pwsh ./go.ps1`; tidy with `pwsh ./clean-up.ps1`.
- Manage packages with `dotnet add/remove package`; never add `Version` attributes.
- Verify SOLID after each C# change; fix violations before proceeding.
- Use README as the source of truth for usage guidance.

## Core Principles

- Canonical scripts/configs trump inferred behavior.
- SOLID and repository guardrails keep generated code review-ready.
- CPM + DI/logging patterns prevent drift across suggestions.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- C#/naming/logging/testing: see respective instruction files under `.github/instructions/`
