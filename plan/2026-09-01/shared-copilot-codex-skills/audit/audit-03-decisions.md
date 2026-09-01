# Decisions

| Decision | Resolution | Evidence | Confidence |
| --- | --- | --- | --- |
| Portable skill target | `.agents/skills/` is the proposed target; #540 owns final cutover | #532/#533; Codex discovery; existing `.github/skills` consumers | High |
| Surface support | Preserve CLI, Codex, Copilot app, VS Code, VS Code Local, cloud agent, code review, Visual Studio, and JetBrains with tiered guarantees | User answer; official GitHub customization matrix | High |
| Always-on rules | Root/nested `AGENTS.md` plus deterministic checks; never skill-only | #532; `AGENTS.md`; shared policies; Clean Squad workflow | High |
| Skill boundary | Recognizable user outcome, not source filename or persona | #532–#540; current agent/instruction duplication | High |
| Delivery | Introduce → redirect/reduce → optional retirement, independently reversible | #532–#540 | High |
| Validation | Structural hard gates offline; bounded live sampling; explicit `PASS`/`FAIL`/`BLOCKED` | review synthesis; repository PowerShell/Pester patterns | High |
| Security | Default-deny capabilities/network, redaction, draft-only publication, isolated live evaluation | security and platform reviews; current privileged gh-aw workflow | High |
| Naming | Stable repository-local lowercase hyphenated skill IDs, distinct from NuGet package names | Agent Skills specification; README package naming | High |
| Versioning | `+semver: skip` only for guidance-only diffs; skill IDs version/deprecate separately | `GitVersion.yml`; compatibility/storage rules | High |
| Runtime scope | No runtime fixes in this programme; protect and report persisted/wire identities | issue scope; event/serialization review; storage rules | High |

## Pilot gate semantics

The #545 result is a real dependency condition:

- `go`: unlock only approved downstream leaves.
- `revise`: invalidate affected drafts/sub-plans until contract, fixtures, or
  boundaries are amended and re-reviewed.
- `stop`: close or defer the affected workstream; no manual bypass.

## CoV

Each decision was cross-checked against issue requirements and at least one
independent repository or official documentation source. The only provisional
choice is the final host cutover mechanics, which #540 must prove before the
pilot.
