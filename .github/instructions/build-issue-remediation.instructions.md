---
applyTo: '**'
---

# Build Issue Remediation Protocol

Governing thought: Agents eliminate build warnings and errors through precise, minimal, iterative edits within a strict attempt limit, deferring difficult issues to preserve momentum.

## Rules (RFC 2119)

- Agents **MUST** make at most five focused fix attempts per single issue before deferring.  
  Why: Prevents infinite loops on difficult issues and maintains forward progress.
- Agents **MUST** define a single issue as a specific warning/error instance in a specific file/location.  
  Why: Prevents scope creep and ensures clear attempt tracking.
- Agents **MUST** leave files in a compiling, consistent state when deferring an issue.  
  Why: Avoids breaking the build for other agents.
- Agents **MUST NOT** refactor broadly; agents **MUST** change only the lines required to remove the issue.  
  Why: Preserves behavior and minimizes risk of introducing regressions.
- Agents **MUST NOT** relax rule severity, remove analyzers, or add project/solution-wide `NoWarn`.  
  Why: This repository has a zero-warnings policy; global suppressions undermine quality standards.
- Agents **MUST NOT** edit generated code; agents **MUST** use the narrowest possible local suppression at the call site with justification if an issue originates from generated files.  
  Why: Generated code should be fixed at the source; local suppressions are safer than bulk changes.
- Agents **MUST** obey `.editorconfig`, `Directory.Build.props/targets`, and solution conventions.  
  Why: Maintains consistency across the codebase.
- Agents **MUST NOT** add package versions to project files.  
  Why: Respects centralized package management defined in `Directory.Packages.props`.
- Agents **MUST NOT** add `[SuppressMessage]` attributes without explicit approval.  
  Why: Analyzer rules exist to enforce quality; suppressions must be justified and rare.
- Agents **MUST** create/update a scratchpad task under `.scratchpad/tasks` with `status=deferred` when deferring an issue.  
  Why: Enables other agents to pick up deferred work with full context.

## Scope and Audience

**Audience:** Agents fixing build warnings, errors, analyzer violations, and cleanup issues.

**In scope:** Compiler errors/warnings, analyzer/StyleCop violations, ReSharper cleanup violations, test/build breaks.

**Out of scope:** Broad refactoring, architectural changes, feature development.

## At-a-Glance Quick-Start

- Build → Clean → Fix until there are zero warnings.

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1
```

- Run tests (Mississippi solution) and mutation tests where required.

```powershell
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

> Mutation tests are intentionally long-running. Wait for `mutation-test-mississippi-solution.ps1` to finish, even if it takes a full 30 minutes.

- Final validation for both solutions.

```powershell
pwsh ./go.ps1
```

> **Drift check:** Before running any PowerShell script referenced here, open the script in `eng/src/agent-scripts/` (or the specified path) to confirm its current behavior matches this guidance. Treat this document as best-effort context—the scripts remain the source of truth for step ordering and options.



## Purpose

This guide helps agents eliminate .NET build issues through iterative, attempt-bounded edits that preserve behavior.

## Core Principles

- Precision over breadth: change only what's necessary to fix the specific issue
- Attempt-bounded iteration: five focused attempts, then defer to maintain forward progress
- Preserve existing `NoWarn` entries maintained by repository owners
- Match existing naming and formatting patterns
- Count attempts per edit-verify cycle; micro-edits within one cycle count as one attempt
- The scratchpad is ephemeral and ignored by Git; do not reference it from source or tests

## Procedures


- Build Mississippi (strict build in Release):

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
```

- Build Samples:

```powershell
pwsh ./eng/src/agent-scripts/build-sample-solution.ps1
```

- Code cleanup and inspections (Mississippi):

```powershell
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1
```

- Unit tests (Mississippi) and mutation tests:

```powershell
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

> Mutation tests are intentionally long-running. Plan for up to 30 minutes and wait for the script to finish.

- Per‑project quick quality loop during iteration:

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name>
```

- Final pipeline (both solutions):

```powershell
pwsh ./go.ps1
```

### Optional raw .NET commands (when you must diagnose outside scripts)

```powershell
dotnet build <solution-or-project> --configuration Release --no-incremental -warnaserror -clp:Summary -nologo
dotnet test <solution-or-project> --configuration Release --no-build -nologo
dotnet format analyzers --verify-no-changes
```

Note: If `dotnet format` reports autofixable warnings, prefer making the minimum equivalent manual edits instead of running bulk formatting.

## Workflow

1) Locate the solution(s)
   - Use `mississippi.slnx` for the core libraries (subject to mutation testing and full quality gates).
   - Use `samples.slnx` for sample apps (minimal tests; no mutation testing required).

2) Reproduce the issues
   - Run the build script(s) to capture the complete issue set. Record warning/error counts grouped by code (e.g., CS8618, CS0168, CA2000).
   - Run cleanup to surface style/inspection violations.

3) Prioritize by impact and safety
   - Target warnings that yield the largest count reduction with the smallest, safe edits:
     - Unused variables/usings and obsolete code paths
     - Nullable reference issues fixable by initializers or guard clauses
     - Disposable usage that needs `using` declarations
     - Async/await mismatches and API usage guidance from analyzers
   - Observe the five‑attempt cap per issue; defer difficult items and proceed to the next target.

4) Plan precision edits
   - For a chosen warning code, inspect 1–3 representative files and define the smallest change that removes the warning without altering public contracts. Prefer:
     - Add missing initializers or constructor assignments
     - Add null checks or `?` where null is valid by design
     - Replace unused locals with `_` or remove when side‑effect free
     - Convert `async` without `await` to sync or add the intended awaited call
     - Wrap disposables in `using` declarations
     - Narrowest `#pragma warning disable CSxxxx` only for true false‑positives or unavoidable third‑party constraints. Restore immediately after the single line/block and add a brief justification comment.

5) Apply changes
   - Edit only the relevant lines. Keep style identical to neighboring code. Do not reformat unrelated code.

6) Verify
   - Rebuild via scripts. Confirm the targeted issues are gone and no new warnings appeared. Re‑run cleanup.

7) Run tests
   - Execute unit tests (and mutation tests for Mississippi projects). All tests must pass.

8) Iterate
   - Repeat steps 3–7 in small batches until warnings reach zero or only narrowly justified suppressions remain. Defer items that exceed five attempts and summarize them for manual follow‑up.

## Multi‑Target and Conditional Cases

- If an issue occurs only for a specific TFM, fix in code first. If configuration is required, condition it by `$(TargetFramework)` in the project file. Avoid global changes affecting other TFMs.
- Do not change language version or nullable context globally unless already standardized in this repository.

## Suppression Rules (Last Resort)

- Prefer local suppression: a single line or the minimal block using `#pragma warning disable/restore CSxxxx` with a brief justification.
- If many identical, legitimate suppressions are required in one file, consider a file‑scoped suppression at the top with justification.
- Project‑level `<NoWarn>` is not allowed.
- Local suppression via `#pragma warning disable/restore CSxxxx` may be used only with explicit approval, scoped to the smallest possible line/block, immediately restored, and accompanied by an exhaustive justification and a tracking issue link.
- If many identical, legitimate suppressions are required in one file (e.g., third‑party constraints), a file‑scoped suppression may be approved case‑by‑case with justification and a tracking issue; prefer fixing code/design first.
- Do not use `[SuppressMessage]` attributes unless explicitly approved under the same criteria as above.

## Examples of Precision Edits

- CS0168 unused variable: replace with `_` or remove the declaration if it has no side effects.
- CS8618 non‑nullable uninitialized: add constructor assignment or default initializer; only use `?` if `null` is valid by design.
- CS1998 async method lacks `await`: remove `async` and return a completed task, or add the intended awaited call.
- CA2000 dispose objects: use `using` declaration or ensure ownership transfer is explicit and safe.
- CS0618 obsolete API: use the recommended alternative; if blocked, add a local suppression with a TODO and link to the tracking issue.

## Output Format for Each Batch

Provide in this order:

1) Commands run
   - Repository scripts (preferred):
   - `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
   - `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
   - `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
   - Raw `dotnet` commands only if used for diagnosis.
2) Before → after warning counts by code (e.g., `CS8618: 14 → 3`, `CA2000: 7 → 0`).
3) Changes as unified diffs per file:

```diff
--- path/to/File.cs
+++ path/to/File.cs
@@

* old line

- new line

```

1. Justification for each change in one sentence.
2. Next targets: the next warning code(s) and files to address.
3. Deferred issues (if any): list `<CODE> @ <path>:<brief reason>, attempts=5`.

## Commit Guidance

- Single, focused commits per warning cohort.
- Commit message format: `fix: resolve <CODE> warnings in <Area> (<n> files)` with a short body listing notable decisions.
