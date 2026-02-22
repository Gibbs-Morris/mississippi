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

## Task 03: Aqueduct.Grains -> Aqueduct.Runtime

- Task type: `ProjectRename`
- Linked pair: `P02`

### Summary
- Renamed the Aqueduct grains runtime project to `Aqueduct.Runtime` and rewired all direct project references while keeping source code behavior unchanged.

### Changes made
- Folder rename: `src/Aqueduct.Grains` -> `src/Aqueduct.Runtime`
- `.csproj` rename: `src/Aqueduct.Runtime/Aqueduct.Grains.csproj` -> `src/Aqueduct.Runtime/Aqueduct.Runtime.csproj`
- `.slnx` updates: updated `mississippi.slnx` project path to the renamed runtime project
- `.csproj` `ProjectReference` updates:
	- `src/Sdk.Silo/Sdk.Silo.csproj`
	- `tests/Aqueduct.Gateway.L0Tests/Aqueduct.Gateway.L0Tests.csproj`
	- `tests/Aqueduct.Gateway.L2Tests/Aqueduct.Gateway.L2Tests.csproj`
	- `tests/Aqueduct.Grains.L0Tests/Aqueduct.Grains.L0Tests.csproj`
	- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
	- `tests/Inlet.Silo.L0Tests/Inlet.Silo.L0Tests.csproj`
- Task-local stabilization (csproj-only): added `<AssemblyName>Mississippi.Aqueduct.Grains</AssemblyName>` in `src/Aqueduct.Runtime/Aqueduct.Runtime.csproj` to preserve assembly identity until `T02`.

### Files touched
- `src/Aqueduct.Runtime/`
- `mississippi.slnx`
- `src/Sdk.Silo/Sdk.Silo.csproj`
- `tests/Aqueduct.Gateway.L0Tests/Aqueduct.Gateway.L0Tests.csproj`
- `tests/Aqueduct.Gateway.L2Tests/Aqueduct.Gateway.L2Tests.csproj`
- `tests/Aqueduct.Grains.L0Tests/Aqueduct.Grains.L0Tests.csproj`
- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
- `tests/Inlet.Silo.L0Tests/Inlet.Silo.L0Tests.csproj`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `see git log (Task P02 commit)`
- Message: `Task P02: rename Aqueduct.Grains project to Aqueduct.Runtime`

### PR-ready notes
- Business value: aligns runtime naming with target architecture and reduces future rename complexity in downstream projects.
- How it works (high level): project folder/file rename plus comprehensive project-reference rewiring in dependent source and test projects.
- Risks / breaking surface: assembly identity drift can break internal test access; mitigated by temporary assembly-name stabilization pending mirrored test rename.
- Follow-up items (if any): execute `T02` immediately after this commit.

## Task 04: Aqueduct.Grains.L0Tests -> Aqueduct.Runtime.L0Tests

- Task type: `TestRename`
- Linked pair: `T02`

### Summary
- Renamed the mirrored Aqueduct runtime L0 test project to align with `P02` and updated solution wiring.

### Changes made
- Folder rename: `tests/Aqueduct.Grains.L0Tests` -> `tests/Aqueduct.Runtime.L0Tests`
- `.csproj` rename: `tests/Aqueduct.Runtime.L0Tests/Aqueduct.Grains.L0Tests.csproj` -> `tests/Aqueduct.Runtime.L0Tests/Aqueduct.Runtime.L0Tests.csproj`
- `.slnx` updates: updated `mississippi.slnx` test project path to renamed project
- `.csproj` `ProjectReference` updates: none beyond path continuity inside renamed project
- Task-local stabilization (csproj-only): added `<AssemblyName>Mississippi.Aqueduct.Grains.L0Tests</AssemblyName>` in `tests/Aqueduct.Runtime.L0Tests/Aqueduct.Runtime.L0Tests.csproj` to preserve internal-access compatibility.

### Files touched
- `tests/Aqueduct.Runtime.L0Tests/`
- `mississippi.slnx`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `see git log (Task T02 commit)`
- Message: `Task T02: rename mirrored Aqueduct.Grains L0 tests to Aqueduct.Runtime.L0Tests`

### PR-ready notes
- Business value: keeps test project naming in lockstep with runtime project naming to reduce confusion and drift.
- How it works (high level): mirrored test folder/project rename plus solution path update and assembly-name stabilization.
- Risks / breaking surface: internal-access failures if assembly identity drifts; mitigated by preserving previous test assembly name in csproj.
- Follow-up items (if any): continue with `P03` then `T03`.

## Task 05: Common.Cosmos.Abstractions -> Common.Runtime.Storage.Abstractions

- Task type: `ProjectRename`
- Linked pair: `P03`

### Summary
- Renamed the Common Cosmos abstractions project to `Common.Runtime.Storage.Abstractions` and updated all direct solution/project references.

### Changes made
- Folder rename: `src/Common.Cosmos.Abstractions` -> `src/Common.Runtime.Storage.Abstractions`
- `.csproj` rename: `src/Common.Runtime.Storage.Abstractions/Common.Cosmos.Abstractions.csproj` -> `src/Common.Runtime.Storage.Abstractions/Common.Runtime.Storage.Abstractions.csproj`
- `.slnx` updates: updated `mississippi.slnx` project path for renamed abstractions project
- `.csproj` `ProjectReference` updates:
	- `src/Common.Cosmos/Common.Cosmos.csproj`
	- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`

### Files touched
- `src/Common.Runtime.Storage.Abstractions/`
- `mississippi.slnx`
- `src/Common.Cosmos/Common.Cosmos.csproj`
- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `see git log (Task P03 commit)`
- Message: `Task P03: rename Common.Cosmos.Abstractions to Common.Runtime.Storage.Abstractions`

### PR-ready notes
- Business value: aligns storage abstractions naming with the target architecture split and improves package intent clarity.
- How it works (high level): folder and project rename with direct dependency rewiring in source and architecture tests.
- Risks / breaking surface: downstream compile references to old project path; mitigated by updating all known direct `ProjectReference` and solution entries.
- Follow-up items (if any): execute `T03` verification task next.

## Task 06: Verify mirrored tests for Common runtime storage abstractions rename

- Task type: `TestRename`
- Linked pair: `T03`

### Summary
- Verified that no dedicated mirrored test project exists for `Common.Cosmos.Abstractions` and recorded this as the required paired test task outcome.

### Changes made
- Folder rename: none
- `.csproj` rename: none
- `.slnx` updates: none
- `.csproj` `ProjectReference` updates: none
- Verification evidence captured in task tracking docs.

### Files touched
- `spec/renaming/task-checklist.md`
- `spec/renaming/pr-description-wip.md`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `<fill after commit>`
- Message: `Task T03: verify no mirrored test project for Common.Runtime.Storage.Abstractions rename`

### PR-ready notes
- Business value: enforces deterministic interleaving while avoiding unnecessary test-project churn.
- How it works (high level): explicit repository search and path verification with recorded outcome.
- Risks / breaking surface: low; documentation and task-tracking only.
- Follow-up items (if any): continue to `P04` then `T04`.

## Task 07: Common.Cosmos -> Common.Runtime.Storage.Cosmos

- Task type: `ProjectRename`
- Linked pair: `P04`

### Summary
- Renamed `Common.Cosmos` to `Common.Runtime.Storage.Cosmos` and rewired all direct source/test references.

### Changes made
- Folder rename: `src/Common.Cosmos` -> `src/Common.Runtime.Storage.Cosmos`
- `.csproj` rename: `src/Common.Runtime.Storage.Cosmos/Common.Cosmos.csproj` -> `src/Common.Runtime.Storage.Cosmos/Common.Runtime.Storage.Cosmos.csproj`
- `.slnx` updates: updated `mississippi.slnx` path for renamed source project
- `.csproj` `ProjectReference` updates:
	- `src/EventSourcing.Brooks.Cosmos/EventSourcing.Brooks.Cosmos.csproj`
	- `src/EventSourcing.Snapshots.Cosmos/EventSourcing.Snapshots.Cosmos.csproj`
	- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
	- `tests/Common.Cosmos.L0Tests/Common.Cosmos.L0Tests.csproj`

### Files touched
- `src/Common.Runtime.Storage.Cosmos/`
- `mississippi.slnx`
- `src/EventSourcing.Brooks.Cosmos/EventSourcing.Brooks.Cosmos.csproj`
- `src/EventSourcing.Snapshots.Cosmos/EventSourcing.Snapshots.Cosmos.csproj`
- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
- `tests/Common.Cosmos.L0Tests/Common.Cosmos.L0Tests.csproj`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Cleanup Mississippi: pass
- Cleanup Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `<fill after commit>`
- Message: `Task P04: rename Common.Cosmos to Common.Runtime.Storage.Cosmos`

### PR-ready notes
- Business value: clarifies storage-runtime boundary and aligns naming with the target package taxonomy.
- How it works (high level): folder/project rename with direct reference rewires in dependent storage projects and architecture tests.
- Risks / breaking surface: compile breaks from stale references; mitigated by updating all direct `ProjectReference` and solution entries.
- Follow-up items (if any): execute `T04` next.

## Task 08: Common.Cosmos.L0Tests -> Common.Runtime.Storage.Cosmos.L0Tests

- Task type: `TestRename`
- Linked pair: `T04`

### Summary
- Renamed the mirrored test project for `Common.Runtime.Storage.Cosmos` to keep the interleaved project/test sequence aligned.

### Changes made
- Folder rename: `tests/Common.Cosmos.L0Tests` -> `tests/Common.Runtime.Storage.Cosmos.L0Tests`
- `.csproj` rename: `tests/Common.Runtime.Storage.Cosmos.L0Tests/Common.Cosmos.L0Tests.csproj` -> `tests/Common.Runtime.Storage.Cosmos.L0Tests/Common.Runtime.Storage.Cosmos.L0Tests.csproj`
- `.slnx` updates: updated `mississippi.slnx` path under `/Tests/Core/`
- `.csproj` updates:
	- `tests/Common.Runtime.Storage.Cosmos.L0Tests/Common.Runtime.Storage.Cosmos.L0Tests.csproj` (added temporary `<AssemblyName>Mississippi.Common.Cosmos.L0Tests</AssemblyName>`)

### Files touched
- `tests/Common.Runtime.Storage.Cosmos.L0Tests/`
- `mississippi.slnx`
- `spec/renaming/task-checklist.md`
- `spec/renaming/pr-description-wip.md`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `<fill after commit>`
- Message: `Task T04: rename Common.Cosmos.L0Tests to Common.Runtime.Storage.Cosmos.L0Tests`

### PR-ready notes
- Business value: keeps mirrored test project names consistent with runtime/storage naming and reduces naming drift across the solution.
- How it works (high level): file-system rename + solution path update, with temporary assembly-name continuity to avoid staged sequence breakage.
- Risks / breaking surface: stale paths in solution or tooling; mitigated by updating `.slnx` and validating with both solution builds and test suites.
- Follow-up items (if any): continue to `P05`.

## Task 09: EventSourcing.Brooks.Abstractions -> Brooks.Abstractions

- Task type: `ProjectRename`
- Linked pair: `P05`

### Summary
- Renamed the Brooks abstractions project to `Brooks.Abstractions` and rewired all direct project references.

### Changes made
- Folder rename: `src/EventSourcing.Brooks.Abstractions` -> `src/Brooks.Abstractions`
- `.csproj` rename: `src/Brooks.Abstractions/EventSourcing.Brooks.Abstractions.csproj` -> `src/Brooks.Abstractions/Brooks.Abstractions.csproj`
- `.slnx` updates: updated `mississippi.slnx` project path under `/Framework/EventSourcing/Brooks/`
- `.csproj` `ProjectReference` updates:
	- `src/Inlet.Silo/Inlet.Silo.csproj`
	- `src/EventSourcing.Sagas.Abstractions/EventSourcing.Sagas.Abstractions.csproj`
	- `src/EventSourcing.UxProjections.Abstractions/EventSourcing.UxProjections.Abstractions.csproj`
	- `src/EventSourcing.Snapshots.Abstractions/EventSourcing.Snapshots.Abstractions.csproj`
	- `src/EventSourcing.Aggregates.Abstractions/EventSourcing.Aggregates.Abstractions.csproj`
	- `src/EventSourcing.Brooks/EventSourcing.Brooks.csproj`
	- `src/EventSourcing.Brooks.Cosmos/EventSourcing.Brooks.Cosmos.csproj`
	- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
	- `tests/Testing.Utilities/Testing.Utilities.csproj`
	- `tests/EventSourcing.Brooks.Abstractions.L0Tests/EventSourcing.Brooks.Abstractions.L0Tests.csproj`
	- `samples/Spring/Spring.Domain/Spring.Domain.csproj`
- Task-local stabilization (csproj-only): added `<AssemblyName>Mississippi.EventSourcing.Brooks.Abstractions</AssemblyName>` to preserve assembly identity until `T05`.

### Files touched
- `src/Brooks.Abstractions/`
- `mississippi.slnx`
- `src/Inlet.Silo/Inlet.Silo.csproj`
- `src/EventSourcing.Sagas.Abstractions/EventSourcing.Sagas.Abstractions.csproj`
- `src/EventSourcing.UxProjections.Abstractions/EventSourcing.UxProjections.Abstractions.csproj`
- `src/EventSourcing.Snapshots.Abstractions/EventSourcing.Snapshots.Abstractions.csproj`
- `src/EventSourcing.Aggregates.Abstractions/EventSourcing.Aggregates.Abstractions.csproj`
- `src/EventSourcing.Brooks/EventSourcing.Brooks.csproj`
- `src/EventSourcing.Brooks.Cosmos/EventSourcing.Brooks.Cosmos.csproj`
- `tests/Architecture.L0Tests/Architecture.L0Tests.csproj`
- `tests/Testing.Utilities/Testing.Utilities.csproj`
- `tests/EventSourcing.Brooks.Abstractions.L0Tests/EventSourcing.Brooks.Abstractions.L0Tests.csproj`
- `samples/Spring/Spring.Domain/Spring.Domain.csproj`

### Verification run
- Build Mississippi: pass
- Build Samples: pass
- Tests Mississippi: pass
- Tests Samples: pass

### Commit
- SHA: `<fill after commit>`
- Message: `Task P05: rename EventSourcing.Brooks.Abstractions to Brooks.Abstractions`

### PR-ready notes
- Business value: simplifies Brooks contract naming and aligns it with the target taxonomy used by follow-on runtime/storage tasks.
- How it works (high level): project folder/file rename plus direct reference rewiring in dependent source, test, and sample projects.
- Risks / breaking surface: stale references and internals access regressions; mitigated by full gate run and temporary assembly-name continuity.
- Follow-up items (if any): execute `T05` immediately.
