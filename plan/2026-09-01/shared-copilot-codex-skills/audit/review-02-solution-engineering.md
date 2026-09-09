# Review 02: Solution Engineering

## Findings

- **High — The skill root remains contradictory.** Resolve `.agents/skills`
  versus `.github/skills` before #544 and add an unowned-root check. Evidence:
  current skill-builder/Rules Manager/docs and Codex/GitHub guidance; confidence
  high.
- **High — Tiered validation needs explicit host rows.** Separate Copilot app
  and VS Code native smoke tests from CLI results, and define discovery,
  invocation, behavior, prerequisites, evidence, owner, and release gate for
  CLI, Codex, app, VS Code, Local, cloud, code review, Visual Studio, and
  JetBrains. Evidence: #540/#543 and official host distinctions; confidence
  high.
- **High — Reproducible Codex probes are unspecified.** Record tested versions,
  configuration/root discovery, explicit/implicit invocation, and context
  results. Evidence: official Codex discovery behavior; confidence high.
- **High — Third-party trust is underspecified.** Define provenance/SHA, license,
  tool/network policy, offline behavior, and rollback for external skills,
  plugins, MCP, or installs. Evidence: official warning that skills are
  unverified; confidence high.
- **High — Onboarding is absent.** Ship a harmless sample skill and copy/paste
  guide with invocation, reload, evidence, and troubleshooting. Evidence:
  README has no agent-customization quick start; confidence high.

## Strengths

The staged migration, outcome boundaries, invariant separation, and honest
secondary-surface intent are strong.

## CoV

The review triangulated issue requirements, repository README/agents, and
official GitHub/Codex customization documentation.
