# Review 11: Source Generator and Tooling Specialist

## Findings

- **High — Define a concrete validator.** Use
  `pwsh ./eng/src/agent-scripts/validate-agent-content.ps1`, Pester coverage,
  machine-readable JSON, stable diagnostics, and a CI entry point.
- **High — Specify YAML parsing.** Pin a real parser/subset and golden fixtures
  for malformed delimiters, duplicates, unknown fields, quotes/multiline,
  BOM/CRLF, and nested metadata.
- **High — Make host discovery executable.** Test each host's root, nesting,
  precedence, unsupported behavior, fallback, reload, and exact supported claim.
- **High — Make case-sensitive validation real.** Use a Linux/WSL/container
  checkout, exact casing, Git-index paths, case/Unicode collision checks, and
  exact relative links.
- **High — Add CI/workflow and graph validation.** Validate triggers, least
  privilege, artifacts, agent frontmatter, handoff/allowlist targets, cycles,
  and generated automation.
- **High — Pin reproducibility inputs.** Record parser/tool/model versions,
  fixture/prompt hashes, normalization, repeat counts, and offline mode.

## Strengths

The plan's cross-platform intent, structural/activation/output separation, and
progressive-disclosure approach are compatible with repository PowerShell/Pester
automation.

## CoV

The review triangulated current frontmatter formats, repository automation,
PowerShell workflows, GitHub/Codex discovery claims, and the draft validator
requirements.
