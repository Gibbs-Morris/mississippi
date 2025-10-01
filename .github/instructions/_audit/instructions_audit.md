# GitHub Copilot Instructions Audit Report

**Generated:** 2025-10-01 02:51:05  
**Repository:** /home/runner/work/mississippi/mississippi  
**Dry Run:** True

## Summary

- **Total Files:** 15
- **Repo-Wide Files:** 1
- **Path-Scoped Files:** 13
- **Agent Files:** 1
- **Rules Extracted:** 845
- **Validation Issues:** 4
- **Duplicate Rules:** 12
- **Safe Edits:** 0
- **Approval Requests:** 0

## Inventory

| File | Type | Size | Last Modified | Frontmatter | ApplyTo | Headings | Match Count | Issues |
|------|------|------|---------------|-------------|---------|----------|-------------|--------|
| `.github/copilot-instructions.md` | repoWide | 3196 | 2025-10-01 | False | - | 5 | 0 | ⚠ 1 |
| `.github/instructions/build-issue-remediation.instructions.md` | pathScoped | 9761 | 2025-10-01 | True | ** | 15 | 999 | ✓ |
| `.github/instructions/build-rules.instructions.md` | pathScoped | 10893 | 2025-10-01 | True | ** | 40 | 999 | ⚠ 1 |
| `.github/instructions/csharp.instructions.md` | pathScoped | 32969 | 2025-10-01 | True | **/*.cs | 33 | 145 | ✓ |
| `.github/instructions/instruction-mdc-sync.instructions.md` | pathScoped | 2992 | 2025-10-01 | True | ** | 9 | 999 | ✓ |
| `.github/instructions/logging-rules.instructions.md` | pathScoped | 68711 | 2025-10-01 | True | **/*.cs | 69 | 145 | ✓ |
| `.github/instructions/mutation-testing.instructions.md` | pathScoped | 8449 | 2025-10-01 | True | ** | 10 | 999 | ✓ |
| `.github/instructions/naming.instructions.md` | pathScoped | 18458 | 2025-10-01 | True | **/*.cs | 31 | 145 | ⚠ 1 |
| `.github/instructions/orleans-serialization.instructions.md` | pathScoped | 10559 | 2025-10-01 | True | **/*.cs | 22 | 145 | ✓ |
| `.github/instructions/orleans.instructions.md` | pathScoped | 13230 | 2025-10-01 | True | **/*.cs | 24 | 145 | ✓ |
| `.github/instructions/projects.instructions.md` | pathScoped | 14211 | 2025-10-01 | True | ** | 31 | 999 | ⚠ 1 |
| `.github/instructions/service-registration.instructions.md` | pathScoped | 46374 | 2025-10-01 | True | **/*.cs | 26 | 145 | ✓ |
| `.github/instructions/test-improvement.instructions.md` | pathScoped | 8405 | 2025-10-01 | True | ** | 13 | 999 | ✓ |
| `.github/instructions/testing.instructions.md` | pathScoped | 12770 | 2025-10-01 | True | ** | 19 | 999 | ✓ |
| `agents.md` | agent | 527 | 2025-10-01 | False | - | 2 | 0 | ✓ |

## Validation Issues
### .github/copilot-instructions.md

- Hygiene: No H1 heading found

### .github/instructions/build-rules.instructions.md

- Hygiene: Multiple H1 headings found (11)

### .github/instructions/naming.instructions.md

- Hygiene: Multiple H1 headings found (10)

### .github/instructions/projects.instructions.md

- Hygiene: Unclosed code block (odd number of ` markers: 31)


## Orphans and Fixes
- **.github/copilot-instructions.md**: ApplyTo globs don't match any files: 
- **agents.md**: ApplyTo globs don't match any files: 


## Duplicate Rules
### Rule appears in 2 files:

> `pwsh ./scripts/build-mississippi-solution.ps1`

Files: .github/instructions/build-issue-remediation.instructions.md, .github/instructions/mutation-testing.instructions.md

### Rule appears in 2 files:

> `pwsh ./scripts/unit-test-mississippi-solution.ps1`

Files: .github/instructions/build-issue-remediation.instructions.md, .github/instructions/mutation-testing.instructions.md

### Rule appears in 2 files:

> Always use Options pattern for configuration — NEVER use direct configuration parameters in constructors; always use `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`

Files: .github/instructions/csharp.instructions.md, .github/instructions/service-registration.instructions.md

### Rule appears in 2 files:

> Build → Clean → Fix until there are zero warnings.

Files: .github/instructions/build-issue-remediation.instructions.md, .github/instructions/build-rules.instructions.md

### Rule appears in 2 files:

> Build Rules (`.github/instructions/build-rules.instructions.md`) - For quality standards, zero warnings policy, and build pipeline requirements that enforce access control analyzer rules

Files: .github/instructions/csharp.instructions.md, .github/instructions/service-registration.instructions.md

### Rule appears in 2 files:

> C# General Development Best Practices (`.github/instructions/csharp.instructions.md`) - For SOLID principles, dependency injection patterns, and immutable object preferences

Files: .github/instructions/naming.instructions.md, .github/instructions/orleans-serialization.instructions.md

### Rule appears in 2 files:

> Final validation for both solutions.

Files: .github/instructions/build-issue-remediation.instructions.md, .github/instructions/build-rules.instructions.md

### Rule appears in 2 files:

> Logging Rules (`.github/instructions/logging-rules.instructions.md`) - For high-performance logging patterns, LoggerExtensions classes, and mandatory ILogger usage with dependency injection properties

Files: .github/instructions/csharp.instructions.md, .github/instructions/service-registration.instructions.md

### Rule appears in 2 files:

> NEVER add `[SuppressMessage]` attributes to hide violations

Files: .github/instructions/build-rules.instructions.md, .github/instructions/csharp.instructions.md

### Rule appears in 2 files:

> NEVER use `#pragma warning disable` without explicit approval and exhaustive justification

Files: .github/instructions/build-rules.instructions.md, .github/instructions/csharp.instructions.md

### Rule appears in 2 files:

> Orleans Best Practices (`.github/instructions/orleans.instructions.md`) - For Orleans-specific grain development patterns, POCO grain requirements, and IGrainBase implementation with sealed classes

Files: .github/instructions/csharp.instructions.md, .github/instructions/service-registration.instructions.md

### Rule appears in 2 files:

> Project File Management (`.github/instructions/projects.instructions.md`) - For proper PackageReference usage and centralized package management

Files: .github/instructions/csharp.instructions.md, .github/instructions/service-registration.instructions.md


## Safe Edits Applied
✓ No safe edits needed.


## Approval Requests
✓ No policy changes requiring approval.

