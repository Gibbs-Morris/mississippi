---
applyTo: '**'
---

# Mutation Testing Playbook

Use this checklist whenever you need to run Mississippi mutation tests or close surviving mutants. Follow these steps exactly; defer architecture, C#, logging, Orleans, and testing nuances to their dedicated instruction files.

## Precedence

1. All repository-wide guidance in `.github/instructions/*.instructions.md` remains authoritative. Resolve conflicts by choosing the most specific file first (feature → language → build). When in doubt, follow the latest modified instruction file without editing it.
2. If you add or modify any instruction Markdown, immediately mirror the change into the Cursor rule set by running `pwsh ./scripts/sync-instructions-to-mdc.ps1` before you finish.

## Execution Loop

1. **Prep tools** – Ensure `dotnet tool restore` has been run at least once on the repo.
2. **Clean build** – Run `pwsh ./scripts/build-mississippi-solution.ps1` to surface compiler/analyzer issues before mutation work.
3. **Baseline mutation test** – Execute `pwsh ./scripts/mutation-test-mississippi-solution.ps1`. Always commit the generated report paths for traceability.
4. **Collect & prioritize survivors** – Open the newest `StrykerOutput/**/reports/mutation-report.json` only if deep inspection is needed. Otherwise run the summarizer (it reruns the mutation script unless you pass `-SkipMutationRun`):
    - `pwsh ./scripts/summarize-mutation-survivors.ps1` (defaults to weighted scoring, full list)
    - Outputs:
       - Basic JSON: `StrykerOutput/mutation-survivors-summary.json` (backward compatibility)
       - Enriched JSON: `StrykerOutput/mutation-survivors-enriched.json` (schemaVersion, focusOrder, aggregates, snippets)
       - Report JSON: `StrykerOutput/mutation-survivors-report.json` (raw survivors from the latest `mutation-report.json` when available)
       - Markdown: `.tests-temp/mutation-survivors-summary.md` (includes prioritized focus table, details, mutator strategy cheat sheet)
    - Optional parameters for focused iteration:
       - `-SkipMutationRun`: Use existing Stryker output without invoking `mutation-test-mississippi-solution.ps1` again.
       - `-MutationScriptPath <Path>`: Point to an alternate mutation script when the default Mississippi script isn't desired.
       - `-Top <N>`: Only keep top N ranked survivors.
       - `-ContextLines <N>`: Embed N lines of code context before/after the mutant (default 3).
       - `-ScoringMode Simple|Weighted`: Simple = no heuristic weights; Weighted = mutator + density + missing-tests weighting.
       - `-Project <Name>`: Filter survivors to a single source project (by directory under `src/`).
       - `-VerboseRanking`: Append a breakdown of score components.
       - `-GenerateTasks`: Emit/update `.tests-temp/mutation-tasks.md` with a table (Status, Rank, Score, File, Mutator, Suggestion).
       - `-TasksPath <Path>`: Override the default `.tests-temp/mutation-tasks.md` location.
       - `-EmitTestSkeletons`: Generate/append mutation test skeletons under the first matching test project `tests/<Project>.*/Mutation/GeneratedMutationTests.cs`.
          - Use `-OverwriteSkeletons` to recreate the file from scratch.
    - Example focused loop (top 5 high-impact):
       ```powershell
       pwsh ./scripts/summarize-mutation-survivors.ps1 -SkipMutationRun -Top 5 -ContextLines 4 -GenerateTasks -EmitTestSkeletons
       ```
    - Each survivor row includes a suggestion (how to kill) and snippet so AI agents can author tests without re-reading the entire file.
5. **Targeted tests only** – Add or adjust tests in the matching `tests/` project using repository patterns (naming, logging, DI seams). Do not change production code unless a mutant is provably unkillable without it; document that case inside the task file.
6. **Quality gates** – After adding tests, run:
   - `pwsh ./scripts/unit-test-mississippi-solution.ps1`
   - `pwsh ./scripts/build-mississippi-solution.ps1`
   Fix every warning or failure before continuing.
7. **Re-run mutation tests** – Execute step 3 again. Update task statuses to `Done` when a mutant dies, or capture justification under `Status = Deferred` if it remains.
8. **Repeat** – Iterate steps 4–7 until no survivors remain or only documented deferrals persist. Keep the task file deterministic (sorted by project, file, line) throughout.

## Completion Checklist

- [ ] Zero surviving mutants or documented exceptions in `.tests-temp/mutation-tasks.md`
- [ ] All builds, unit tests, and cleanup scripts pass with zero warnings
- [ ] Instruction Markdown and Cursor `.mdc` rules are in sync via `sync-instructions-to-mdc.ps1`

## Prioritized Survivor Workflow (Recommended)

1. Run baseline mutation test (Execution Loop step 3).
2. Generate ranked survivors (top N for fast focus):
   ```powershell
   pwsh ./scripts/summarize-mutation-survivors.ps1 -SkipMutationRun -Top 10 -GenerateTasks -EmitTestSkeletons
   ```
3. Open `.tests-temp/mutation-tasks.md`; take the first `Todo` (Rank 1 highest score).
4. Fill in or extend the generated skeleton in `GeneratedMutationTests.cs` (or create a dedicated test if more appropriate).
5. Run fast quality loop for the impacted test project:
   ```powershell
   pwsh ./scripts/test-project-quality.ps1 -TestProject <Project>.Tests -SkipMutation
   ```
6. If test passes and kills survivor, re-run summarizer (with `-SkipMutationRun` if you have not rerun Stryker yet) or re-run full mutation test to validate.
7. Repeat until `focusOrder` empty or only justifiable deferrals remain.

## Mutator Strategy Reference

| Mutator | What It Usually Means | How To Kill It |
| ------- | --------------------- | -------------- |
| ConditionalBoundary | Missing exact edge (==, <=, >=) case | Add boundary input(s) asserting both sides of branch |
| NegateCondition | Only one branch asserted | Add test where predicate is false/alternate path |
| ArithmeticOperator | Numeric result loosely validated | Assert precise arithmetic across varied (negative/zero/boundary) inputs |
| LogicalOperator | Not all clause combinations covered | Add permutations to toggle each clause independently |
| Boolean | Single boolean path exercised | Add opposite boolean scenario |
| String | Insufficient string content validation | Assert full string (case, whitespace, format) |
| Assignment | State change not asserted | Assert mutated property/field after operation |
| RemoveCall / MethodCall | Side-effect or call result unverified | Assert side-effect or verify interaction / returned value |

Use the summarizer's suggestion column as an immediate next action hint; refine assertions rather than broadening mocks.

## Task File Notes

- `.tests-temp/mutation-tasks.md` is ephemeral; regenerate anytime with `-GenerateTasks`.
- Update `Status` manually to `Done` or `Deferred` (include justification for deferrals—design constraints, unreachable path, or verified false positive).
- Keep ordering stable (do not manually reorder ranked entries unless you re-run with different filters).

## Test Skeleton Guidance

- Generated skeletons are placed in `Mutation/GeneratedMutationTests.cs` to avoid cluttering domain-focused test files.
- Replace `Assert.True(true)` with meaningful assertions quickly—leaving placeholders risks masking assertion gaps.
- If a skeleton requires significant setup, consider extracting a helper builder or fixture; keep it in the test project.

## Schema Versioning

- Enriched JSON (`mutation-survivors-enriched.json`) includes `schemaVersion` (current: `1.1.0`).
- Backward compatible basic JSON array persists for existing automation.
- When extending schema: bump minor version, document new properties, do not remove existing ones without deprecation notice.

## Deferral Criteria

Defer only when ALL apply:
- Mutant cannot be killed without a non-trivial production refactor AND
- Behavior is already thoroughly asserted from a business perspective AND
- Refactor risk/time outweighs value for current iteration.

Record deferral rationale inline in `.tests-temp/mutation-tasks.md` (add brief reason after the row or in a trailing comment block).
