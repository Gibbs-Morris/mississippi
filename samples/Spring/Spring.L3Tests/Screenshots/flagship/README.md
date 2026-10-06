# Spring flagship browser evidence

These captures come from the real local Spring application, with its generated commands, Orleans domain, configured storage and SignalR projections.
The [journey map](../../../JOURNEYS.md) connects each capability to its prerequisite, implementation and regression.

## Source and validation

The manifests record the committed application source, client and Refraction Git trees, browser/tool versions, launch mode, routes, viewports, themes and capture times.
The evidence commit adds documentation and captures to that tested source.
The client and Refraction tree IDs must match before treating these images as evidence for another revision.

The [validation record](validation/results.json) lists the canonical Release, L2 Full and L3 Full results, baseline results and mutation status.
Doctor's READY result checks prerequisites and executes no tests.
The Release pipeline excludes deployed L2/L3 tests.

The Release process log is a bounded capture, not a complete transcript.
Its fresh per-project raw TRX counts were independently audited; historical reports accumulated by the pipeline are excluded from the reported executed totals.
[Core counters](validation/core-unit-counters.json) and [sample counters](validation/sample-unit-counters.json) retain the original raw-report paths and SHA-256 values.
The versioned full-suite TRX files retain each actual L2/L3 test result.

Baseline comparisons cover all five original routes and populated banking at both sizes.
Their [manifest](baseline/manifest.json) explicitly leaves unrecorded browser versions, capture times and exact helper commands unknown.
They are pre-edit comparison evidence, separate from the final On/Off runs.

## Reproduce the captures

Run from the repository root with the SDK in global.json, PowerShell 7 and Docker Linux access, plus Node.js and npm for the capture helper.
Keep the interactive app running in a separate terminal:

```powershell
pwsh ./run-spring.ps1 -LocalAuth On
```

The capture helper has optional Node dependencies, isolated from the repository's build dependencies:

```powershell
npm install --prefix artifacts/tools/accessibility --no-save playwright@1.62.1 @axe-core/playwright@4.13.0
$env:SPRING_PLAYWRIGHT_MODULE = (Resolve-Path artifacts/tools/accessibility/node_modules/playwright).Path
$env:SPRING_AXE_MODULE = (Resolve-Path artifacts/tools/accessibility/node_modules/@axe-core/playwright).Path
$env:PLAYWRIGHT_BROWSERS_PATH = (Join-Path (Get-Location) 'artifacts/tools/playwright')
node artifacts/tools/accessibility/node_modules/playwright/cli.js install chromium
$env:SPRING_SOURCE_REF = (git rev-parse HEAD).Trim()
$env:SPRING_CLIENT_TREE = (git rev-parse HEAD:samples/Spring/Spring.Client).Trim()
$env:SPRING_REFRACTION_TREE = (git rev-parse HEAD:src/Refraction.Client).Trim()
$env:SPRING_LOCAL_AUTH = 'On'
$env:SPRING_URL = 'http://localhost:5101'
$env:SPRING_CAPTURE_DIR = 'artifacts/spring-redesign/capture-on'
node samples/Spring/Spring.L3Tests/Screenshots/flagship/capture.cjs
```

Set SPRING_URL to the gateway URL printed by your launcher.
The source identifiers describe the running app; commit edits and restart its launcher before claiming new source provenance.
The helper prints each capture result and writes manifest.json for journey/assertion failures after browser setup.
Startup failures, such as missing modules, invalid source IDs or browser setup failure, may produce only stderr and no manifest.
Only PASS with completed=true means all terminal assertions completed.

Stop the app with Ctrl+C, launch with -LocalAuth Off, then set SPRING_LOCAL_AUTH to Off and use a new SPRING_CAPTURE_DIR to reproduce the protected-endpoint comparison.
All five browser personas receive 401 with local auth off.
Stop the app before running any validation script in the same worktree.

## What the captures check

The On run covers all five routes at desktop and phone sizes; first-run setup; initial loading; a genuinely pending command; acceptance followed by real projection updates; a second browser; validation; a failed withdrawal; completed and compensated transfers; investigations; all themes; forced colors; connection details, offline/reconnection, labels, focus and narrow/long content.
It selects and reselects all five personas, checks their actual HTTP reads and authenticated commands, and delays an older allowed response until after a new denial.
Full L3 also checks every protected command and saga-start result for each persona, all six burst controls, exact balances and ledger retention.

The lost-reply journey forwards one real deposit to the server, records its actual 200/success result, then aborts delivery to the browser.
It verifies the visible client failure, real increased balance and exactly one ledger entry.
It expands command history at 390px and 320px, verifies containment and keyboard access, and scrolls the labelled region with ArrowRight.
The helper never supplies invented backend response data and does not retry the command.

Axe checks WCAG A/AA tags through WCAG 2.2; its incomplete targets and console/network observations are retained in the manifests.
Human inspection checks forced-colors labels, focus, layout and contrast as well as automatic results.
Phone evidence uses Chromium viewports with touch enabled; physical devices, other browser engines and screen readers are outside this run.
MCP registration and Reservoir DevTools integration are source-verified; a connected MCP client and browser extension are not exercised by this helper.

## Review dispositions

Independent architecture, CoV and UX reviewers read the changed source and inspect the real journeys and captures.
The [review dispositions](reviews/dispositions.json) identify each verified finding, correction and validation evidence, including source coverage after cleanup.
The [expanded-history regression](validation/history-regression.json) failed at both phone widths before its two-declaration grid fix, then passed without weakening the actual lost-reply assertions.
Its [before](validation/history-before.trx) and [after](validation/history-after.trx) reports retain those actual outcomes.
Hosted CI and human PR review remain separate from this local evidence.

## Captured journeys

| Task or state | Phone | Desktop |
| --- | --- | --- |
| First visit | [Start here](on/home-phone.png) | [Start here](on/home-desktop.png) |
| Prepare accounts | [Setup](on/accounts-phone.png) | [Setup](on/accounts-desktop.png) |
| Both opening projections observed | [£500 each](on/accounts-ready-phone.png) | [£500 each](on/accounts-ready-desktop.png) |
| Loading and pending | [Initial reads](on/accounts-loading-phone.png), [awaiting a command response](on/command-pending-phone.png) | Covered by real browser assertions |
| Transfer and domain failure | [Completed](on/transfer-completed-phone.png), [rejected withdrawal](on/withdrawal-rejected-phone.png), [invalid draft](on/invalid-transfer-phone.png) | [Completed transfer](on/transfer-completed-desktop.png) |
| Defined compensation | [Source reversal and saga phase](on/transfer-compensated-phone.png) | Covered by real browser assertions |
| High-value deposit | [Investigation queue](on/investigation-populated-phone.png) | [Investigation queue](on/investigation-populated-desktop.png) |
| Permission and obsolete response | [Unauthenticated](on/auth-unauthenticated-phone.png), [older allowed read ignored](on/auth-obsolete-read-ignored-phone.png) | [Raw snapshots](on/auth-snapshots-desktop.png) |
| Themes | [Dark](on/money-dark-phone.png), [Light](on/money-light-phone.png), [High Contrast](on/money-high-contrast-phone.png), [forced colors](on/money-forced-colors-phone.png) | Existing full L3 theme assertions |
| Narrow, long and disconnected | [320px](on/money-narrow-phone.png), [long account](on/long-account-phone.png), [offline](on/connection-offline-phone.png) | Independent browser replay |
| Lost reply after a real commit | [Failure and live outcome](on/lost-reply-committed-phone.png), [expanded history at 320px](on/lost-reply-history-narrow-phone.png) | Independent browser replay |
| LocalAuth Off | [Protected endpoint comparison](off/auth-off-full-access-phone.png) | [Protected endpoint comparison](off/auth-off-full-access-desktop.png) |

[On manifest](on/manifest.json) and [Off manifest](off/manifest.json) retain exact routes, viewports, source identifiers, tools, assertions, axe incomplete targets and console/network observations.
[Asset inventory](asset-inventory.json) records packaged file sizes and hashes; it excludes itself.
PNG hashes use the original binary bytes; text hashes use UTF-8 without a BOM and LF line endings so a Windows checkout does not change their comparison.
Raw TRX hashes in the validation counters describe the original executed files and are separate from this portable text representation.
