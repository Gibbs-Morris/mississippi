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

## Task 02: Aqueduct.* test projects -> Aqueduct.Gateway.* test projects

- Task type: `TestRename`
- Linked pair: `T01`

### Summary
- Renamed the mirrored Aqueduct test projects to match the completed `P01` project rename and rewired solution and test-project references accordingly.

### Changes made
- Folder rename: `tests/Aqueduct.L0Tests` -> `tests/Aqueduct.Gateway.L0Tests`; `tests/Aqueduct.L2Tests` -> `tests/Aqueduct.Gateway.L2Tests`; `tests/Aqueduct.L2Tests.AppHost` -> `tests/Aqueduct.Gateway.L2Tests.AppHost`
- `.csproj` rename: `Aqueduct.L0Tests.csproj` -> `Aqueduct.Gateway.L0Tests.csproj`; `Aqueduct.L2Tests.csproj` -> `Aqueduct.Gateway.L2Tests.csproj`; `Aqueduct.L2Tests.AppHost.csproj` -> `Aqueduct.Gateway.L2Tests.AppHost.csproj`
- `.slnx` updates: updated `mississippi.slnx` test project paths to renamed test projects
- `.csproj` `ProjectReference` updates:
	- `tests/Aqueduct.Gateway.L2Tests/Aqueduct.Gateway.L2Tests.csproj` (AppHost project path)
- Task-local stabilization (csproj-only): added `<AssemblyName>Mississippi.Aqueduct.L0Tests</AssemblyName>` in `tests/Aqueduct.Gateway.L0Tests/Aqueduct.Gateway.L0Tests.csproj` to preserve InternalsVisibleTo assembly identity during rename.

### Files touched
- `tests/Aqueduct.Gateway.L0Tests/`
- `tests/Aqueduct.Gateway.L2Tests/`
- `tests/Aqueduct.Gateway.L2Tests.AppHost/`
- `mississippi.slnx`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `see git log (Task T01 commit)`
- Message: `Task T01: rename mirrored Aqueduct test projects to Aqueduct.Gateway.*`

### PR-ready notes
- Business value: keeps project and test naming topology aligned so future refactors and package identities remain predictable.
- How it works (high level): mirrored folder/project renames, solution entry rewiring, and targeted test project reference updates.
- Risks / breaking surface: test assembly identity can break `InternalsVisibleTo`; mitigated by preserving original L0 test assembly name in csproj.
- Follow-up items (if any): continue with `P02` then `T02` in sequence.
