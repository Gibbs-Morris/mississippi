# Scratchpad Task Helpers

This folder contains PowerShell utilities that automate the standard `.scratchpad/tasks` workflow described in `.github/instructions/agent-scratchpad.instructions.md`. The scripts are thin wrappers over `ScratchpadTasks.psm1`, which holds the shared functions for locating the scratchpad, validating structure, and reading/writing task JSON files.

## Scripts

| Script | Description |
| --- | --- |
| `new-scratchpad-task.ps1` | Creates a new task file under `.scratchpad/tasks/pending` with normalized metadata (schema version, ID, UTC timestamps, priority, tags, attempts counter). |
| `list-scratchpad-tasks.ps1` | Lists tasks across statuses with optional filters (status, priority, tags, ID) and supports including the raw JSON payload. |
| `claim-scratchpad-task.ps1` | Atomically moves a pending task to `tasks/claimed`, stamps the `claimedBy`/`claimedAt` fields, and increments the `attempts` counter (enforcing the 5-attempt limit). |
| `complete-scratchpad-task.ps1` | Marks a claimed task as `done`, records the result summary and completion timestamp, and moves the file into `tasks/done`. |
| `defer-scratchpad-task.ps1` | Moves a pending or claimed task into `tasks/deferred`, capturing the deferral reason, optional next steps, and deferral timestamp. |

## Module

`ScratchpadTasks.psm1` exposes reusable helpers:

- `Resolve-ScratchpadRoot`, `Initialize-ScratchpadLayout`, and `Get-ScratchpadPaths` for locating and preparing the scratchpad structure.
- `Read-ScratchpadTask`, `Write-ScratchpadTask`, and `Find-ScratchpadTaskById` for safe JSON IO and lookups.
- `ConvertTo-ScratchpadTaskRecord` for projecting the stored JSON into a friendly object shape.

The CLI scripts above import this module automatically; if you want to compose your own tooling, you can import it manually with `Import-Module ./eng/src/agent-scripts/tasks/ScratchpadTasks.psm1`.

## Testing

PowerShell tests live in `../../tests/agent-scripts`:

- `run-scratchpad-task-tests.ps1` runs the Pester suite (`scratchpad-task-scripts.Tests.ps1`).
- `verify-scratchpad-task-scripts.ps1` performs an end-to-end smoke test (create → claim → complete/defer) against a temporary scratchpad and cleans up afterwards.

Run tests with PowerShell 7+ and ensure the Pester module is available: `pwsh ./eng/tests/agent-scripts/run-scratchpad-task-tests.ps1`.
