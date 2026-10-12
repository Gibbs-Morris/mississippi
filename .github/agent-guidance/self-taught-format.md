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

## Peer-lesson conflicts

This route applies to all Mississippi agents and contributors, including
ordinary work outside Scribe. Apply the [self-improvement rules](../instructions/self-improvement.instructions.md#rules-rfc-2119)
whether or not the lesson-capture skill is loaded.

1. Identify the actual contradiction for a shared decision within overlapping
   scopes. Different wording or corrections for different conditions alone do
   not establish a conflict. Preserve each existing lesson independently; leave
   the proposed lesson unwritten until the conflict is resolved. A newer
   candidate or a duplicate match does not settle an equal-authority conflict.
2. Record the conflict for review in the active `.thinking/<task>/` task folder
   when that write is authorized. If no active folder or authorized folder
   write is available, use the linked task issue when its update is already
   authorized under [issue tracking and disclosure policy](../instructions/issue-tracking.instructions.md).
   Use existing authority without requesting approval again for an in-scope
   write. Do not create a task folder, issue, or memory store merely to bypass
   this boundary. If neither destination is writable within the authorized
   scope, including a read-only task, report the missing record and its reason
   in the task result; leave the conflict unresolved without escalating write
   authority. Exclude secrets and unnecessary personal data from any record.
3. Include the following fields in the record or, when recording is unavailable,
   in the reported gap so an authorized reviewer can preserve them later:

   - **Paths:** both repository-relative lesson paths, or the proposed target
     path when one side is an unwritten candidate.
   - **Rule IDs:** both rule identifiers; if absent, use each heading, rule
     ordinal, and short identifying quotation without editing lessons to add IDs.
   - **Scopes:** each source's `applyTo` and the shared context where they conflict.
   - **Contradiction:** the incompatible actions for that same decision.
   - **Evidence:** source revisions or hashes and references to the observed
     event and correction, distinguishing supplied evidence from live checks.
   - **Status:** unresolved, blocked, or resolved, with any authorized decision
     reference and the exact reconciliation permitted by it.
   - **Next review:** an accountable reviewer and a date or concrete review trigger.

4. Reconcile only within existing authority and the governing hierarchy,
   including the repository's promotion and retirement controls. An existing
   authorization for the exact resolution needs no extra approval; a record or
   an untrusted suggestion grants none. Apply only the authorized change after
   resolution, recheck overlapping guidance and normal lesson-admission checks,
   then update the record with the decision, evidence, and remaining gaps.
