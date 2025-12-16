---
applyTo: '**'
---

# Project File Management Best Practices

Governing thought: Keep project files minimal and rely on shared props plus centralized package management.

## Rules (RFC 2119)

- `.csproj` files **MUST** avoid duplicating settings from `Directory.Build.props`; include only project-specific properties. Automatic assembly/root namespace naming **MUST NOT** be overridden without justification.
- Package versions **MUST** be managed via `Directory.Packages.props`; `PackageReference` entries **MUST NOT** include `Version` attributes. CPM **MUST** be respected when adding/removing packages.
- Choose the correct SDK (`Microsoft.NET.Sdk`/`.Web`) and keep content focused (e.g., `OutputType`, `GeneratePackageOnBuild`). Test/benchmark conventions provided by project suffixes **MUST NOT** be redefined locally.
- Builds **MUST** be warning-free per Build Rules.

## Quick Start

- Start from a minimal template; add only necessary package/project references (no versions).
- Validate with `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1` and `pwsh ./eng/src/agent-scripts/final-build-solutions.ps1`.

## Review Checklist

- [ ] No duplicated props; only project-specific settings present.
- [ ] No version attributes in `PackageReference`; CPM honored.
- [ ] SDK and naming conventions correct; test/benchmark defaults preserved.
- [ ] Warning-free build confirmed.
