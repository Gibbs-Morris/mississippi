---
applyTo: '**'
---

# Agent Scratchpad: Local State Between Prompts

Governing thought: The `.scratchpad/` folder provides ephemeral, file-based coordination between agents using atomic task files with deterministic lifecycles to avoid conflicts and enable parallel work.

## Rules (RFC 2119)

- Agents **MUST NOT** store secrets, credentials, or PII in `.scratchpad/`.  
  Why: The folder is untracked and ephemeral; security-sensitive data belongs in secure vaults.
- Agents **MUST NOT** reference `.scratchpad/` paths from source code, projects, or tests.  
  Why: Prevents accidental coupling between production artifacts and ephemeral coordination state.
- Agents **MUST** use atomic file moves to claim tasks from `tasks/pending/` to `tasks/claimed/`.  
  Why: Ensures exactly-one agent ownership without race conditions.
- Task owners **MUST** update only their claimed task file; other agents **MUST NOT** modify claimed files.  
  Why: Prevents concurrent edit conflicts and maintains clear ownership.
- Agents **MUST** use UTC ISO-8601 timestamps in all task JSON files.  
  Why: Eliminates timezone ambiguity for distributed coordination.
- Agents **MUST** respect the 5-attempt limit per task before moving to `tasks/deferred/`.  
  Why: Aligns with Build Issue Remediation policy and prevents infinite retry loops.
- Task files **MUST** use the naming pattern `<yyyyMMddHHmmss>_<slug>_<ulid>.json`.  
  Why: Provides sortable, globally unique identifiers for conflict-free parallel task creation.
- Agents **SHOULD** keep task files small and line-oriented; agents **SHOULD NOT** store large binaries.  
  Why: Facilitates diff-friendly version control and reduces storage overhead.
- Workers **SHOULD** sort pending tasks by `priority`, then FIFO by filename timestamp.  
  Why: Ensures urgent work is processed first while maintaining fairness for same-priority tasks.
- Producers **SHOULD** slice large work into tasks ≤ 15 minutes each.  
  Why: Enables better parallelization and reduces wasted effort if a task must be deferred.
- Agents **MAY** prune old `runs/` and completed tasks at any time.  
  Why: The `.scratchpad/` is ephemeral; long-term records belong in issues, PRs, or docs.

## Scope and Audience

**Audience:** All agents coordinating work in the repository workspace.

**In scope:** Task lifecycle, file naming, atomicity rules, and operational patterns for `.scratchpad/`.

**Out of scope:** Build artifacts, source code, CI configuration, or durable repository state.

## At-a-Glance Quick-Start

- Location: `.scratchpad/` (ignored by Git; only `.scratchpad/.gitignore` is tracked)
- Pattern: file-per-item folders to avoid concurrent edits to a single file
- Tasks lifecycle folders: `tasks/pending → tasks/claimed → tasks/done|tasks/deferred`
- Claiming work: atomically move a file within `.scratchpad/tasks` to claim it
- Identity: name files `<yyyyMMddHHmmss>_<slug>_<ulid>.json` for sortability + uniqueness
- Schema: small JSON objects with `schemaVersion`, stable keys, ISO-8601 timestamps
- Never commit: no code, secrets, or durable records belong here

> Drift check: If you change instruction Markdown, mirror changes to Cursor rule files via `pwsh ./eng/src/agent-scripts/sync-instructions-to-mdc.ps1` (see Instruction ↔ Cursor MDC Sync Policy). Scripts remain the source of truth for automation; this file conveys the repository policy and expected usage.

## Purpose

The `.scratchpad/` folder is a local, untracked workspace where agents can coordinate state between prompts and runs. Use it to exchange task queues, claim work, record run notes, and persist small, disposable artifacts. Nothing in `.scratchpad/` is part of the source tree or build; treat it as ephemeral.

Safe to clear at any time without impacting the repository.

## Core Principles

- File-per-task design enables atomic claims without central coordination
- JSON with stable keys remains tooling-friendly and easy to diff
- Ephemeral location prevents accidental coupling between code and agent state
- Respect repository rules: zero-warnings policy, coding standards, and test gates apply to code, not to scratchpad contents

## Directory Layout

```text
.scratchpad/
  tasks/
    pending/           # New tasks ready to be claimed (one JSON file per task)
    claimed/           # Claimed by an agent (ownership + in-progress metadata)
    done/              # Completed tasks (result summary included)
    deferred/          # Deferred tasks with reason and next steps
  testing/
    mutation-survivors-summary.md  # Markdown summary formerly in .tests-temp
    mutation-tasks.md              # Survivor task table formerly in .tests-temp
  runs/
    latest/            # Optional symlink/marker to latest run directory
    20251004-140500_agentA/  # One folder per agent run
      summary.json     # What was attempted, status, high-level stats
      log.md           # Human-readable notes for handoff
      diff.patch       # Optional: unified diff(s) for reference only
  agents/
    registry.json      # Optional: known agent ids, capabilities, preferences
    locks/             # Optional: advisory lock files if needed (see Concurrency)
```

## File Naming and Schema

- File name: `<yyyyMMddHHmmss>_<slug>_<ulid>.json`
  - Example: `20251004T140501_fix-stylecop_01J8RX5Z0S7X6WQ2P8BH2PZ35N.json`
  - ULID recommended for sortable uniqueness; GUID acceptable if ULID generation isn’t available

- Minimal task JSON schema (example):

```json
{
  "schemaVersion": "1.0",
  "id": "01J8RX5Z0S7X6WQ2P8BH2PZ35N",
  "title": "Resolve CA2000 in Core project",
  "createdAt": "2025-10-04T14:05:01Z",
  "priority": "P2",
  "tags": ["build", "analyzers"],
  "status": "pending",
  "claimedBy": null,
  "claimedAt": null,
  "attempts": 0,
  "notes": "See build-issue-remediation guidance; cap at 5 attempts"
}
```

- When claimed or completed, update only the file you move (see Lifecycle). Keep keys stable and append new fields rather than changing meanings. Prefer ISO-8601 timestamps.

## Lifecycle and Workflow

1) Producer agent creates tasks as JSON files under `tasks/pending/` (one file per task)
2) Worker agent claims a task by atomically moving the file to `tasks/claimed/` and annotating ownership
3) On success, move to `tasks/done/` with `completedAt` and a short `result` summary
4) If blocked after focused effort, move to `tasks/deferred/` with `reason` and `nextSteps`

Attempt limits: align with Build Issue Remediation — at most 5 focused attempts per task before deferring. Record the reason in the task file.

### Example updates when claiming and finishing

- On claim (modify JSON fields in the same file after moving):
  - `status`: `claimed`
  - `claimedBy`: `agent-name`
  - `claimedAt`: `2025-10-04T14:12:00Z`
  - `attempts`: increment

- On completion (after work, move into `done/`):
  - `status`: `done`
  - `completedAt`: `2025-10-04T14:42:15Z`
  - `result`: short human-readable summary; link to artifacts if any

## Deterministic Agent Contract

Follow these rules so multiple agents behave predictably and converge on the same outcomes.

- Invariants
  - One file = one task; ownership is the folder path, not a field alone
  - Claim by atomic move only; never edit a file still under `pending/`
  - Only the owner updates a claimed task file; others must not modify it
  - Timestamps are UTC ISO-8601; do not use local time
  - Append new fields instead of changing meanings; bump `schemaVersion` only on breaking change

- Producer (task author) obligations
  - Slice large work into uniform, small tasks (target ≤ 15 minutes each); set `effortPoints` if helpful (1–5)
  - Provide precise titles, tags, and a minimal reproducible note in `notes`
  - Prefer many small tasks over few big ones; avoid cross-file mega-tasks

- Worker (task executor) obligations
  - Claim via atomic move (`pending` → `claimed`) and immediately set `claimedBy`, `claimedAt`, and increment `attempts`
  - Respect the 5-attempt limit; on exhaustion, move to `deferred` with `reason` + `nextSteps`
  - On success, move to `done` and add a concise `result` summary and `completedAt`
  - Do not “re-open” done items; create a new pending task referencing the prior `id` if follow-up is required

## Status and Priority Model

- `status`: one of `pending`, `claimed`, `done`, `deferred`, `cancelled`, `superseded`
  - `cancelled`: no longer relevant (document why)
  - `superseded`: replaced by another task (include `supersededBy` id)
- `priority`: `P0` (urgent/outage), `P1` (time-sensitive), `P2` (normal), `P3` (nice-to-have)
- Optional sizing: `effortPoints` integer (1–5)

Recommended extra fields

```json
{
  "effortPoints": 2,
  "relatedFiles": ["src/Feature/File.cs:42", "tests/Feature.Tests/File.Tests.cs"],
  "references": [".github/instructions/build-rules.instructions.md"],
  "supersededBy": null,
  "completedAt": null,
  "result": null,
  "reason": null,
  "nextSteps": null
}
```

## JSON Schema (optional, for tooling)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "ScratchpadTask",
  "type": "object",
  "required": ["schemaVersion", "id", "title", "createdAt", "priority", "status", "attempts"],
  "properties": {
    "schemaVersion": {"type": "string"},
    "id": {"type": "string"},
    "title": {"type": "string"},
    "createdAt": {"type": "string", "format": "date-time"},
    "priority": {"type": "string", "enum": ["P0", "P1", "P2", "P3"]},
    "tags": {"type": "array", "items": {"type": "string"}},
    "status": {"type": "string", "enum": ["pending", "claimed", "done", "deferred", "cancelled", "superseded"]},
    "claimedBy": {"type": ["string", "null"]},
    "claimedAt": {"type": ["string", "null"], "format": "date-time"},
    "attempts": {"type": "integer", "minimum": 0},
    "effortPoints": {"type": ["integer", "null"], "minimum": 1, "maximum": 5},
    "relatedFiles": {"type": "array", "items": {"type": "string"}},
    "references": {"type": "array", "items": {"type": "string"}},
    "completedAt": {"type": ["string", "null"], "format": "date-time"},
    "result": {"type": ["string", "null"]},
    "reason": {"type": ["string", "null"]},
    "nextSteps": {"type": ["string", "null"]},
    "supersededBy": {"type": ["string", "null"]}
  }
}
```

## Examples: When to Use the Scratchpad

- Build issue backlog slicing
  - Producer runs the build scripts, groups warnings by code and file, and emits one task per fixable instance or small cohort (per Build Issue Remediation). Workers claim and fix in parallel.

- Test improvement, coverage gaps, and mutation survivors
  - Coverage tasks are generated automatically by rerunning `summarize-coverage-gaps.ps1`, which syncs `.scratchpad/tasks/pending` with the latest Cobertura results. Mutation survivor tasks continue to be generated automatically by `summarize-mutation-survivors.ps1`. Reserve manual JSON creation for edge cases not handled by these scripts.

- Logging conformance
  - Producer scans for direct `ILogger.Log*` calls and creates tasks to convert to LoggerExtensions patterns per logging rules.

- Naming and access-control cleanups
  - Producer enumerates StyleCop SA13xx/SA16xx and access control violations, emitting small, discrete tasks per file or rule.

- Project hygiene
  - Producer identifies project files with drift from `Directory.Build.props`/CPM patterns and emits a task per project to normalize.

## PowerShell Recipes (optional helpers)

These snippets standardize create/claim/complete flows. Adjust as needed.

```powershell
$Scratch = Join-Path (Get-Location) ".scratchpad"
$Pending = Join-Path $Scratch "tasks/pending"
$Claimed = Join-Path $Scratch "tasks/claimed"
$Done    = Join-Path $Scratch "tasks/done"
$Deferred= Join-Path $Scratch "tasks/deferred"

function New-ScratchpadTask {
  param(
    [Parameter(Mandatory)] [string]$Title,
    [string[]]$Tags = @(),
    [ValidateSet('P0','P1','P2','P3')] [string]$Priority = 'P2',
    [int]$EffortPoints = 2,
    [string]$Notes = ''
  )
  $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
  $id = [Guid]::NewGuid().ToString()
  $slug = ($Title -replace '[^a-zA-Z0-9]+','-').Trim('-').ToLower()
  $file = Join-Path $Pending "$stamp`_${slug}_$id.json"
  $task = [ordered]@{
    schemaVersion = '1.0'
    id = $id
    title = $Title
    createdAt = (Get-Date).ToUniversalTime().ToString('o')
    priority = $Priority
    tags = $Tags
    status = 'pending'
    claimedBy = $null
    claimedAt = $null
    attempts = 0
    effortPoints = $EffortPoints
    notes = $Notes
  }
  New-Item -ItemType Directory -Force -Path $Pending | Out-Null
  ($task | ConvertTo-Json -Depth 5) | Out-File -FilePath $file -Encoding UTF8 -NoNewline
  $file
}

function Claim-ScratchpadTask {
  param([Parameter(Mandatory)] [string]$Path, [string]$Agent = $env:USERNAME)
  $dest = Join-Path $Claimed (Split-Path $Path -Leaf)
  New-Item -ItemType Directory -Force -Path $Claimed | Out-Null
  try { Move-Item -LiteralPath $Path -Destination $dest -ErrorAction Stop } catch { throw "Task already claimed or missing: $Path" }
  $json = Get-Content -Raw -LiteralPath $dest | ConvertFrom-Json
  $json.status = 'claimed'
  $json.claimedBy = $Agent
  $json.claimedAt = (Get-Date).ToUniversalTime().ToString('o')
  $json.attempts = [int]$json.attempts + 1
  ($json | ConvertTo-Json -Depth 8) | Set-Content -Path $dest -Encoding UTF8
  $dest
}

function Complete-ScratchpadTask {
  param([Parameter(Mandatory)] [string]$Path, [Parameter(Mandatory)] [string]$Result)
  $json = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
  $json.status = 'done'
  $json.completedAt = (Get-Date).ToUniversalTime().ToString('o')
  $json.result = $Result
  ($json | ConvertTo-Json -Depth 8) | Set-Content -Path $Path -Encoding UTF8
  $dest = Join-Path $Done (Split-Path $Path -Leaf)
  New-Item -ItemType Directory -Force -Path $Done | Out-Null
  Move-Item -LiteralPath $Path -Destination $dest
  $dest
}

function Defer-ScratchpadTask {
  param([Parameter(Mandatory)] [string]$Path, [Parameter(Mandatory)] [string]$Reason, [string]$NextSteps = '')
  $json = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
  $json.status = 'deferred'
  $json.reason = $Reason
  $json.nextSteps = $NextSteps
  ($json | ConvertTo-Json -Depth 8) | Set-Content -Path $Path -Encoding UTF8
  $dest = Join-Path $Deferred (Split-Path $Path -Leaf)
  New-Item -ItemType Directory -Force -Path $Deferred | Out-Null
  Move-Item -LiteralPath $Path -Destination $dest
  $dest
}
```

## Operational Tips

- Chunking: if a producer generates N tasks, keep each small and independent; prefer ≤ 100 tasks per wave
- Fairness: workers sort pending tasks by `priority`, then FIFO by timestamp in filename
- Back-pressure: if `claimed/` grows large, pause new claims until `done/` advances
- Hygiene: prune `runs/` older than a week unless actively referenced
- Automation: use the CLI wrappers under `eng/src/agent-scripts/tasks/` to keep JSON consistent and file moves atomic:
  - `new-scratchpad-task.ps1` creates pending tasks with normalized schema and unique IDs.
  - `list-scratchpad-tasks.ps1` surfaces pending/claimed/done work with filters for status, priority, tags, or ID and can optionally output the raw payload.
  - `claim-scratchpad-task.ps1` moves a pending task to `claimed/`, stamps `claimedBy`/`claimedAt`, and enforces the five-attempt limit from Build Issue Remediation.
  - `complete-scratchpad-task.ps1` records a result summary, timestamps completion, and moves the file to `done/`.
  - `defer-scratchpad-task.ps1` captures the deferral reason/next steps and moves the file to `deferred/`.
  - For orchestration or custom automation, review `eng/src/agent-scripts/tasks/README.md` plus the Pester suite in `eng/tests/agent-scripts/`.

## Concurrency and Atomicity

- Use atomic file moves (rename within the same filesystem) to claim tasks:
  - Move from `tasks/pending/…json` to `tasks/claimed/…json`
  - If the move succeeds, the agent owns the task; if it fails due to missing source, select another task
- Avoid shared mutable files; keep “one file = one unit of ownership” to minimize contention
- Advisory locks are rarely needed. If required, use short-lived `.lock` files in `agents/locks/` and delete them promptly

## Runs and Handoffs

- Each agent run may write a `runs/<stamp>_<agent>/summary.json` and `log.md` to provide context for handoffs
- Include:
  - What was attempted (commands/scripts run)
  - Which tasks were claimed, completed, or deferred
  - Any diffs or file lists created during the run (reference only; do not commit)

## Cleanup and Retention

- `.scratchpad/` is ephemeral. You may prune old `runs/` and completed tasks at any time
- Keep only what’s helpful for immediate coordination; long-term records belong in issues, PR descriptions, or docs

## PR and Policy Checklist

- [x] Root `.gitignore` ignores `.scratchpad/` and re-includes `.scratchpad/.gitignore`
- [x] `.scratchpad/.gitignore` ignores all transient files in that folder
- [ ] If you change this instruction, mirror to Cursor `.mdc` via `eng/src/agent-scripts/sync-instructions-to-mdc.ps1`
- [ ] No source code, tests, or CI workflows depend on `.scratchpad/`
