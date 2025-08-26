---
applyTo: '**'
---

# Build Issue Remediation Protocol

This guide helps agents eliminate .NET build issues (errors, analyzer/StyleCop warnings, cleanup violations) with precise, minimal edits that preserve behavior and align with repository standards.

## At-a-Glance Quick-Start

- Build → Clean → Fix until there are zero warnings.
```powershell
pwsh ./scripts/build-mississippi-solution.ps1
pwsh ./scripts/clean-up-mississippi-solution.ps1
```
- Run tests (Mississippi solution) and mutation tests where required.
```powershell
pwsh ./scripts/unit-test-mississippi-solution.ps1
pwsh ./scripts/mutation-test-mississippi-solution.ps1
```
- Final validation for both solutions.
```powershell
pwsh ./go.ps1
```

## Intent

When asked to “fix build issues” (warnings, analyzer findings, StyleCop, cleanup), follow this protocol to make precise, minimal edits that remove issues while preserving behavior and style.

## Scope

- Compiler errors and warnings
- Analyzer and StyleCop violations
- ReSharper cleanup violations detected by `./scripts/clean-up-mississippi-solution.ps1`
- Test/build breaks surfaced by the repository scripts

## Guardrails

- Do not refactor broadly. Change only the lines required to remove the issue.
- Do not relax rule severity, remove analyzers, or add project/solution-wide `NoWarn`. This repository has a zero‑warnings policy. Existing `NoWarn` entries maintained by repository owners may remain; the agent must not add or modify `NoWarn`.
- Do not edit generated code. If an issue originates from generated files, use the narrowest possible, local suppression at the call site with justification.
- Obey `.editorconfig`, `Directory.Build.props/targets`, and solution conventions. Match existing naming and formatting.
- Respect centralized package management. Do not add package versions to project files.
- Do not add `[SuppressMessage]` attributes; analyzer rules must be satisfied unless explicitly approved.

## Attempt Limits and Deferral

- Apply a strict attempt cap: make at most five focused fix attempts per single issue before deferring.
- Define a single issue as a specific warning/error instance in a specific file/location (e.g., `CS8618` for `Customer.Name` assignment in `Customer.cs`). Do not aggregate across files/locations.
- Count an attempt when you perform an edit and run a verify step (build/cleanup/tests). Micro-edits within the same verify cycle count as one attempt.
- On reaching five attempts without a clean verify, stop working that issue, leave the file in a compiling, consistent state, and defer it.
- Record every deferred issue (with code, file, brief reason) and move on to the next highest‑impact fix.

## Commands to Use (Repository Standard)

- Build Mississippi (strict build in Release):
```powershell
pwsh ./scripts/build-mississippi-solution.ps1
```
- Build Samples:
```powershell
pwsh ./scripts/build-sample-solution.ps1
```
- Code cleanup and inspections (Mississippi):
```powershell
pwsh ./scripts/clean-up-mississippi-solution.ps1
```
- Unit tests (Mississippi) and mutation tests:
```powershell
pwsh ./scripts/unit-test-mississippi-solution.ps1
pwsh ./scripts/mutation-test-mississippi-solution.ps1
```
- Per‑project quick quality loop during iteration:
```powershell
pwsh ./scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation
pwsh ./scripts/test-project-quality.ps1 -TestProject <Name>
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
     - `pwsh ./scripts/build-mississippi-solution.ps1`
     - `pwsh ./scripts/clean-up-mississippi-solution.ps1`
     - `pwsh ./scripts/unit-test-mississippi-solution.ps1`
   - Raw `dotnet` commands only if used for diagnosis.
2) Before → after warning counts by code (e.g., `CS8618: 14 → 3`, `CA2000: 7 → 0`).
3) Changes as unified diffs per file:
```
--- path/to/File.cs
+++ path/to/File.cs
@@

* old line

- new line

```
4) Justification for each change in one sentence.
5) Next targets: the next warning code(s) and files to address.
6) Deferred issues (if any): list `<CODE> @ <path>:<brief reason>, attempts=5`.

## Commit Guidance

- Single, focused commits per warning cohort.
- Commit message format: `fix: resolve <CODE> warnings in <Area> (<n> files)` with a short body listing notable decisions.

## Related Guidelines

- Build Rules and Quality Standards (`.github/instructions/build-rules.instructions.md`)
- Testing Strategy and Quality Gates (`.github/instructions/testing.instructions.md`)
- Test Improvement Workflow (`.github/instructions/test-improvement.instructions.md`)
- Project File Management (`.github/instructions/projects.instructions.md`)
- Instruction ↔ Cursor MDC Sync Policy (`.github/instructions/instruction-mdc-sync.instructions.md`)


