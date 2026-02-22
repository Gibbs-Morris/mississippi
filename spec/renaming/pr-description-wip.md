# PR Description WIP Notes (Renaming)

Use this file as the running source of truth for the final PR description.

## How to use

- After each task is completed, append a new section using the template below.
- Keep entries factual and scoped to that single task/commit.
- Capture enough detail to assemble PR sections later (business value, implementation, files changed, verification, risks).

## Task Entry Template

```markdown
## Task <N>: <OldProject> -> <NewProject>

- Task type: `ProjectRename` | `ProjectMerge` | `ProjectExtract` | `TestRename` | `TestMerge` | `TestExtract`
- Linked pair: `<P## or T##>`

### Summary
- What was renamed and why.

### Changes made
- Folder rename:
- `.csproj` rename:
- `.slnx` updates:
- `.csproj` `ProjectReference` updates:

### Files touched
- `path/to/file-or-folder`

### Verification run
- Build Mississippi: pass/fail
- Build Samples: pass/fail
- Cleanup Mississippi: pass/fail
- Cleanup Samples: pass/fail
- Tests Mississippi: pass/fail
- Tests Samples: pass/fail

### Commit
- SHA:
- Message:

### PR-ready notes
- Business value:
- How it works (high level):
- Risks / breaking surface:
- Follow-up items (if any):
```

## Entries

## Task 01: Aqueduct -> Aqueduct.Gateway

- Task type: `ProjectRename`
- Linked pair: `P01`

### Summary
- Renamed the Aqueduct gateway project folder and project file to align with the target naming plan while keeping code changes out of scope.

### Changes made
- Folder rename: `src/Aqueduct` -> `src/Aqueduct.Gateway`
- `.csproj` rename: `src/Aqueduct.Gateway/Aqueduct.csproj` -> `src/Aqueduct.Gateway/Aqueduct.Gateway.csproj`
- `.slnx` updates: updated `mississippi.slnx` project path to new project location
- `.csproj` `ProjectReference` updates:
	- `tests/Aqueduct.L0Tests/Aqueduct.L0Tests.csproj`
	- `tests/Aqueduct.L2Tests/Aqueduct.L2Tests.csproj`
	- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
	- `src/Inlet.Server/Inlet.Server.csproj`
- Task-local stabilization (csproj-only): added `<AssemblyName>Mississippi.Aqueduct</AssemblyName>` in `src/Aqueduct.Gateway/Aqueduct.Gateway.csproj` so the task validates before `T01`.

### Files touched
- `src/Aqueduct.Gateway/`
- `mississippi.slnx`
- `src/Inlet.Server/Inlet.Server.csproj`
- `tests/Aqueduct.L0Tests/Aqueduct.L0Tests.csproj`
- `tests/Aqueduct.L2Tests/Aqueduct.L2Tests.csproj`
- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `see git log (Task P01 commit)`
- Message: `Task P01: rename Aqueduct project to Aqueduct.Gateway`

### PR-ready notes
- Business value: aligns project identity and solution topology with the agreed naming model.
- How it works (high level): project path/file rename plus explicit reference rewiring in solution and dependent projects.
- Risks / breaking surface: assembly identity drift between project and test names; currently mitigated with temporary assembly-name stabilization until `T01`.
- Follow-up items (if any): execute `T01` immediately next to rename mirrored Aqueduct test projects.
