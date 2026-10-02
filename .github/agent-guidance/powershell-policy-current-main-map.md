# PowerShell policy preservation map

This current-main slice replaces only the PowerShell portion of historical
[PR #809](https://github.com/Gibbs-Morris/mississippi/pull/809). The existing
`.github/instructions/powershell.instructions.md` filename and `**/*.ps*`
selector remain. The adapter requires a complete read of `eng/AGENTS.md`,
including for PowerShell content outside `eng/`; skill activation is not needed.

| Current-main source | Mandatory destination | Preserved condition |
| --- | --- | --- |
| Rule 1: shebang, strict mode, stop-on-error | `eng/AGENTS.md` PS1, PS1.2 | Every matching script; fail-fast settings cannot relax |
| Rule 2: explicit process exit | `eng/AGENTS.md` PS2, PS2.2 | Every matching script; no implicit success |
| Rule 3: no hidden global state or swallowed helper errors | `eng/AGENTS.md` PS3, PS3.2 | Scripts and modules |
| Rule 4: typed, validated parameters and shared helpers | `eng/AGENTS.md` PS4, PS4.2 | Recommendation for PowerShell automation |
| Rule 5: cross-platform paths and structured output | `eng/AGENTS.md` PS5, PS5.2 | Recommendation for portable scripts and consumers |
| Quick start: template, input checks, module scope, test runner | `eng/AGENTS.md` PS6 | Workflow remains executable on current main |

The test command is the current `eng/tests/orchestrate-powershell-tests.ps1`.
The historical reference to the unmerged `verify-change` binding is omitted;
current `testing.instructions.md` and `build-rules.instructions.md` remain
available for validation selection. No PowerShell implementation changes.
