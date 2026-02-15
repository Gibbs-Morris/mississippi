# CoV Mississippi Issue Ledger

Use this ledger to prevent repeat fixes and to escalate difficulty over time.

## Entries

| Date (UTC) | Agent Persona | Issue Category | File Path(s) | Pattern/Rule (short) | Fix Summary | Verification Evidence (tests/commands) | Commit / PR link |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 2026-02-15 | CoV Mississippi Codebase Sweep (Fix 5) | Process lifecycle bug | `run-lightspeed.ps1` | Stale process cleanup must handle `dotnet`-hosted apps | Added `Stop-LightSpeedDotnetProcessByProjectPath` to terminate stale `dotnet` processes for `LightSpeed.AppHost` and `LightSpeed.Server` by project path, preventing port/process conflicts. | `pwsh ./run-lightspeed.ps1` (repro), `get_errors` on edited files | `40db526a` |
| 2026-02-15 | CoV Mississippi Codebase Sweep (Fix 5) | Startup reliability bug | `run-lightspeed.ps1` | Avoid duplicate build in run step | Updated launch to `dotnet run --project ... -c Debug --no-build` after explicit solution build to remove redundant rebuild and reduce launch flakiness. | `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1`, `get_errors` on edited files | `40db526a` |
| 2026-02-15 | CoV Mississippi Codebase Sweep (Fix 5) | Environment leakage | `run-lightspeed.ps1` | Scripts should restore caller environment | Wrapped run flow in `try/finally` and restored all modified environment variables (`ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT`, logging overrides) to prevent shell pollution across commands. | Script review + `get_errors` on `run-lightspeed.ps1` | `40db526a` |
| 2026-02-15 | CoV Mississippi Codebase Sweep (Fix 5) | Navigation UX bug | `samples/LightSpeed/LightSpeed.Client/Pages/Index.razor` | Blazor internal nav should use router links | Replaced raw anchor with `NavLink` for `/kitchen-sink` to keep SPA navigation client-side (no full page reload). | `get_errors` on edited files | `40db526a` |
| 2026-02-15 | CoV Mississippi Codebase Sweep (Fix 5) | Accessibility regression risk | `samples/LightSpeed/LightSpeed.Client/App.razor` | Route changes should move focus to heading | Added `<FocusOnNavigate RouteData="@routeData" Selector="h1" />` in router `Found` block to improve keyboard/screen-reader route transitions. | `get_errors` on edited files | `40db526a` |

## Do Not Repeat

### Previously Fixed Patterns

- `run-lightspeed.ps1`: process cleanup by executable name only for `dotnet`-hosted projects.
- `run-lightspeed.ps1`: duplicate build (`dotnet build` + `dotnet run` without `--no-build`).
- `run-lightspeed.ps1`: script-level environment variable leakage after execution.
- `LightSpeed.Client/Pages/Index.razor`: raw internal navigation anchor used instead of router link.
- `LightSpeed.Client/App.razor`: missing focus management on route navigation.

### Previously Touched Hotspots

- `run-lightspeed.ps1`
- `samples/LightSpeed/LightSpeed.Client/App.razor`
- `samples/LightSpeed/LightSpeed.Client/Pages/Index.razor`

### Closed-Out Trivial Categories

- None (this run prioritized runtime reliability and accessibility/navigation behavior).

## Maintenance Rules

- Read this ledger before selecting issues.
- Do not pick an issue if its category+pattern+path is already closed unless new evidence shows a deeper root cause.
- Append exactly five new table rows per run (one per fix).
- Update Do Not Repeat with newly closed patterns, hotspots, and trivial categories.