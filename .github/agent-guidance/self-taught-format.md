# Self-Taught Lesson Format

## Domain Categories

| Domain | `applyTo` | Covers |
|--------|-----------|--------|
| `build` | `'**'` | Build pipeline, MSBuild, project files, NuGet, CI |
| `testing` | `'tests/**'` | Test patterns, coverage, mutation, determinism |
| `csharp` | `'**/*.cs'` | C# idioms, compiler behavior, analyzers |
| `serialization` | `'**/*.cs'` | Orleans serialization, JSON, wire formats |
| `orleans` | `'**/*.cs'` | Grains, activation, lifecycle, hosting |
| `agent-workflow` | `'.github/agents/**'` | Agent design, workflow steps, prompt engineering |
| `documentation` | `'docs/**'` | Docusaurus, page structure, MDX |
| `powershell` | `'**/*.ps*'` | Scripts, engineering tools |
| `blazor` | `'**/*.razor*'` | Blazor UI, components, SignalR |

## Self-Taught File Template

Use this structure for new `self-taught-<domain>.instructions.md` files:

````markdown
---
applyTo: '<matching pattern for domain>'
---

# Self-Taught Lessons: <Domain>

Governing thought: Empirical lessons learned from real-work failures in <domain area>, captured to prevent repeated mistakes.

> Drift check: Before adding a lesson, read all instruction files whose `applyTo` overlaps with this file's scope; verify no conflict with hand-authored rules per `self-improvement.instructions.md`.

## Rules (RFC 2119)

- <RFC 2119 keyword> <concise lesson>. Why: <evidence from the observed failure>.

## References

- Self-improvement governance: `.github/instructions/self-improvement.instructions.md`
- <relevant domain instruction file(s)>
````
