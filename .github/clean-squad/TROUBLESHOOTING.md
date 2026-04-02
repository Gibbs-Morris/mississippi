# Clean Squad Troubleshooting

Use this guide when Clean Squad customizations load incorrectly, route unexpectedly, or fail parity checks.

## Quick parity check

Run:

```powershell
pwsh -File ./eng/validate-clean-squad-customizations.ps1
```

This validates:

- workflow roster vs manifest membership
- frontmatter identity
- public visibility
- internal worker visibility
- `agents:` allowlists and `agents: []` denials
- public handoff targets

## Symptom -> checks -> likely cause -> recovery

### A protected worker appears in the agent picker

#### Checks

1. Open the worker's `.agent.md` file.
2. Confirm `user-invocable: false`.
3. Run the parity validator.

#### Likely cause

- Frontmatter drift or a failed merge resolution.

#### Recovery

1. Restore the correct frontmatter from `customization-manifest.json`.
2. Re-run the validator.
3. Re-open Chat Customizations diagnostics in VS Code if the picker still shows stale state.

### River or a coordinator can delegate to the wrong agent

#### Checks

1. Inspect the active agent's `agents:` allowlist.
2. Compare it with `customization-manifest.json`.
3. Re-run the parity validator.

#### Likely cause

- Allowlist drift or a manual edit that widened the coordinator surface.

#### Recovery

1. Restore the allowlist to the manifest value.
2. Keep any new delegation target out of service until `WORKFLOW.md` is updated first.

### Nested coordinator did not fan out

#### Checks

1. Confirm the coordinator is one of the approved nested coordinators.
2. Confirm `chat.subagents.allowInvocationsFromSubagents` is enabled.
3. Check whether the coordinator recorded degraded mode intentionally.

#### Likely cause

- Nested subagents are disabled, blocked, or intentionally bypassed because artifacts already existed.

#### Recovery

1. Enable the setting when the environment allows it.
2. If policy forbids nested subagents, keep the degraded direct-delegation path and do not widen the roster.

### Prompt file could not find the documented tool sets

#### Checks

1. Confirm whether a profile-level `.toolsets.jsonc` file exists.
2. Compare it with `.github/clean-squad/clean-squad.toolsets.template.jsonc`.
3. Remember that current VS Code tool sets are profile-level, not workspace-loaded.

#### Likely cause

- The current profile has not installed the template yet.

#### Recovery

1. Copy the template into your prompts home as `<name>.toolsets.jsonc`.
2. Re-open VS Code or the Chat tools picker.
3. If you do nothing, prompts still work; VS Code ignores unavailable tool-set names.

### Hook did not run

#### Checks

1. Confirm hooks are allowed by policy in your VS Code environment.
2. Confirm `.github/hooks/clean-squad-customization-guard.json` is present.
3. Check the **GitHub Copilot Chat Hooks** output channel.

#### Likely cause

- Hooks are disabled by policy, unsupported in the current environment, or the edit did not touch one of the watched customization surfaces.

#### Recovery

1. Treat hooks as optional.
2. Run `/clean-squad-validate-contracts` or the validator script manually.

## Canary and rollback guidance

### Canary slice

Use this minimum smoke set after meaningful customization changes:

1. `cs Entrepreneur` public invocation
2. `cs River Orchestrator` public invocation
3. one protected worker remains hidden
4. one approved phase coordinator shows the correct allowlist behavior
5. one prompt file loads
6. one skill is discoverable

### Rollback threshold

Rollback the current slice if any of these happen:

- a third public Clean Squad agent appears
- River loses a required worker or gains an unauthorized one
- a protected worker becomes casually invocable
- workflow and manifest parity cannot be restored quickly

### Rollback steps

1. Revert the changed customization files.
2. Re-run `pwsh -File ./eng/validate-clean-squad-customizations.ps1`.
3. Re-open VS Code customization diagnostics.
4. Re-run the canary slice.

## Compatibility notes

- Nested subagents require `chat.subagents.allowInvocationsFromSubagents`.
- Hook behavior is preview and optional.
- Tool sets are currently profile-level `.toolsets.jsonc` files, so the repo ships a template rather than a workspace-loaded authoritative config.
- Parent repository discovery may be needed when teams open a subfolder instead of the repo root.
