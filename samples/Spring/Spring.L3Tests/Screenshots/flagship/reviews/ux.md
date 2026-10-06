# Independent final Spring UX review

The final On/Off experience has no unresolved verified UX finding in this review scope. The previous expanded-history phone overflow is closed by a fresh real-data replay at 320 and 390 pixels. Every 52 final root PNG, every 22 independent runtime PNG and every 12 manual contrast-probe PNG was inspected. The complete packaged evidence snapshot was independently reviewed with no open finding; the parent’s final incorporation metadata will be rechecked separately.

Source 31ceb637f0ebd8998f201be19f4cb541ffcbb3bb; client fa5eb13866948f72d008776e1e25b2328ae8974e; Refraction 8465aae40c12f0ada4edeae9809d2c10e4cccf9b; base 957ea8d8f631eac556d56da5d831702115bec49c. Chromium 151.0.7922.34, Playwright 1.62.1, axe 4.13.0. The report remains bound to this code source even if a later commit only packages unchanged evidence.

## Actual browser journeys

- PASS: Unique first-run accounts selected with trimmed IDs, prerequisite/invalid initial amount, real £500 opening balances and skip focus
- PASS: Keyboard jump B transfers actual focus after bare-route navigation
- PASS: Keyboard jump A transfers actual focus after bare-route navigation
- PASS: Touch jump transfers actual Account B panel focus
- PASS: Close control wrapping and no overflow at 390
- PASS: Close control wrapping and no overflow at 320
- PASS: Connection name includes visible status and Close restores trigger focus
- PASS: Native Control click opens real new tab with selected pair/fragment and preserves original link focus
- PASS: Keyboard selection of three themes, invalid draft prevention, forced-colors native primary and phone overflow
- PASS: Real deposit, ledger, second-browser updates, completed saga, withdrawal, failure outcome, preserved burst/tool discoverability
- PASS: Fresh entity actual 404 guidance, accepted command, real GET catch-up count 1, same-persona fresh 200, denied 401 with no stale count and separate accepted history
- PASS: Offline notice and actual fresh second-browser £1 deposit delivered after reconnect
- PASS: Real committed lost-reply history is contained and keyboard scrollable at 390
- PASS: Real committed lost-reply history is contained and keyboard scrollable at 320
- PASS: One real HTTP 200/success deposit with lost browser reply shows client failure, uncertain server outcome, both browsers £501 and exactly one added £1 ledger row; no retry

The independent replay used unique explicit fictional accounts/entity IDs and low-value real commands. A single real deposit returned HTTP 200/success on the server before its browser reply was deliberately aborted. Both browsers then showed £501, and the ledger increased from 4 to 5 rows with exactly one new £1 entry. No command was retried. Tab focused the named history region and ArrowRight moved its internal scroll. Region widths were 314/600 at 390 and 244/600 at 320, while document width matched the viewport. Keyboard Account A/B jumps from a bare route transferred actual panel focus; native Control-click opened the full selected-pair href in a real new tab and preserved original link focus.

All reviewer browsers are closed. Source, app processes, builds and GitHub were read-only.

## Manual screenshots and accessible states

The final On manifest completed PASS with 41 captures, 46 PNGs and 40 axe audits. Off completed PASS with 6 captures/PNGs/audits. There were zero actual axe violations, page exceptions or document-overflow records. Every initial route, prerequisite/loading/pending/validation/success/failure/permission state, live balance/ledger, completed/compensated saga, theme, forced-colors control, long content, shared pair and developer tools was inspected. Initial Investigations images show loading rather than settled empty. The deliberate browser-offline image was not axe-audited.

Narrow manifest states use 320×740 viewports; their actual PNG widths are 320. Full-page PNG heights exceed 740 and are retained separately from viewport dimensions in report.json.

## All 19 axe incomplete targets

The only incomplete rule was color-contrast because a horizontal scroller obscured its targets: five investigation cells/headers, six 390px history targets and eight 320px history targets. Every exact selector was exposed and measured on the fresh 31 build. All 12 left/middle/right/full probe images were inspected. Header/caption rgb(162, 165, 169) over effective pane background rgb(14.44, 18.44, 22.24) measures 7.58:1; cell/request-ID rgb(242, 242, 243) measures 16.75:1. All targets were readable after scrolling. The named regions accepted keyboard focus and ArrowRight moved scrollLeft 0→40. History probe used a fresh equivalent actual OpenAccount500/lost-reply Deposit1 pair and preserved the real server 200/success/one request/£501/one ledger entry evidence. This closes these specific manual contrast/access targets, not a general WCAG certification.

## Every console/network record

On has 92 records: 30 HTTP, 30 network and 32 console. HTTP 400×1 is the deliberate insufficient-funds withdrawal; 401×5 protected unauthenticated requests; 403×6 role-profile denied reads; 404×18 specific fresh/unopened/not-yet-projected account/auth/saga/queue reads. Network ERR_ABORTED×28 matches streamed missing/denied projection GETs by exact URL/count; AutoProjectionFetcher uses ResponseHeadersRead and disposes those responses without reading bodies. ERR_INTERNET_DISCONNECTED×1 is the intentional hub-offline probe; ERR_FAILED×1 the deliberately lost reply after one actual server-accepted deposit. Console status/failure counts match exactly, but individual pairing is inferential because location/URL/phase were not recorded.

Off has 61 records: 27 actual HTTP 401 protected requests, 27 matching generic console statuses and seven projection-response disposal aborts. All five personas actually receive 401 for each of three commands, saga start and protected read. No count is displayed. Off prerequisite and mode limitation are readable in all 6 images. Every original record and URL-specific disposition is retained in report.json; no broad status whitelist was used.

## Closed findings

- Full selected-pair account links and actual keyboard/touch panel focus; native modified-click new-tab behavior retained
- Connection accessible name contains visible changing status
- Close label fits 320/390 and restores trigger focus
- Forced-colors primary labels use readable native ButtonFace/ButtonText
- Same-persona protected read performs a fresh actual GET and settles with real count
- Correlated entity/persona/request read ignores obsolete allowed result after newer denial; separate raw snapshots remain discoverable
- Protected outcome status locator selects p[role=status] rather than output implicit status
- AuthProof accepted-command projection catch-up requires bounded GET-only response/body/rendered evidence
- Failure wording distinguishes client failure from uncertain server outcome after a real lost reply
- Expanded real Banking history is contained at 320/390 with Tab focus and actual ArrowRight scrolling; one real committed lost-reply deposit adds exactly one ledger row and no command retry
- Packaged capture instructions explicitly require Node.js and npm in addition to the app SDK, PowerShell and Docker prerequisites
- Packaged capture instructions distinguish journey/assertion failures after browser setup, which retain a manifest, from bootstrap failures that may leave stderr only

## Complete packaged evidence audit

All 102 packaged files were checked: 101 indexed entries plus the inventory itself. All 76 packaged PNGs are byte-identical to an original already manually inspected: 52 final On/Off, 12 baseline and 12 contrast-probe images. All 26 nonimage documents were read, including the README, tested capture harness, manifests, source/review/finding ledgers, process records, counters, results and baseline/history qualifications. All 19 JSON documents parse; all four XML reports were parsed and their actual results/counters inspected. All 41 local Markdown links and 27 finding-evidence links resolve. All eight source/runtime finding dispositions are resolved.

The current source ledgers agree on all 76 logical source paths and their present/deleted bytes. Fresh raw unit evidence contains 3461 passing core tests in 38 nonzero reports and 280 passing sample tests in four reports; three zero-test facade reports are excluded. L2 Full has 15 passes and L3 Full 20 passes. The retained history-before report has two real containment failures and no skips; later explicit server assertions were not reached there. History-after has two passes and no skips; final Full L3 executes those full assertions. Doctor remains readiness only, mutation remains not run, bounded Go output is disclosed, and portable text normalization is distinct from raw executed-report hashes. These were evidence reads, not reviewer build/test executions.

Both evidence-document findings are closed: Node.js/npm prerequisites are explicit, and the manifest guarantee is limited to journey/assertion failures after browser setup while bootstrap failures may report only stderr. The PR body draft's journey/demo/screenshot and accessibility/error limits match this evidence; its final commit/count placeholders and final incorporation metadata await the parent’s planned last recheck. Historical pending review checkpoints remain historical. The final reviewer-report/disposition/inventory incorporation does not change the verified application source or claim additional runtime coverage.

## Provenance and limits

The baseline 12 PNGs were also independently inspected and hashed. Parent recorded pre-edit commit ddc892cd4c117362fab71e0f564b71774feb6f20 at discovery/launch; the capture helper did not instrument source/version/time/command. Those missing fields remain unknown. Baseline populated desktop A500/B catching up differs from the both £500 phone moment. Historical a1 evidence is kept unchanged and is not final-source proof.

UX, accessibility, task/outcome semantics and harness assertions reviewed across iterations. This reviewer does not claim independent review of every changed repository file; other independent reviewers own complete architecture/file coverage.

- Chromium 151 emulated 320/390 phone and 1440 desktop. No physical touch device, screen reader, Safari or Firefox evidence.
- All 46 On, 6 Off, 22 independent runtime and 12 manual-incomplete probe PNGs were visually inspected. Original detail requested; the 6067px independent advanced-tools image was scaled to 6000px by the image viewer.
- Root On 40/41 states and all six Off states were axe-audited. Deliberate browser-offline state was not axe-audited. All 19 clipped color-contrast targets were manually checked; this is not a general WCAG certification.
- Independent full runtime used fresh unique explicit IDs and actual commands, and did not mutate default accounts or high-value/global-queue state. Investigation contrast probe read actual current global data without a command.
- Independent full runtime covers real keyboard/touch/Control-new-tab/live reconnect/same-persona denial and lost reply. Full five-persona command/role/saga matrix, compensation and obsolete allowed-read race additionally rely on parent capture/assertions and prior source review; own final runtime did not independently reorder reads.
- Off runtime is parent capture plus independent manifest/source/rendered review; this reviewer did not repeat a separate Off browser experiment.
- Console capture omits location/URL/phase. Individual console/request pairing is qualified inference from exact status/failure-group counts; every HTTP/network record was classified by actual URL and exercised state.
- No blanket 4xx/network whitelist was used. Nonzero expected console/network errors remain disclosed in the report.
- Historical a1 source evidence remains historical and unchanged. Baseline source identity is discovery-record provenance, not instrumented capture identity; exact browser/version/time/command remain unknown.
- The full packaged evidence snapshot was independently reviewed with no open finding. Parent incorporation of the final reviewer reports, root disposition and inventory still requires the planned metadata recheck. Repository/hosted gates and PR publication readiness remain parent responsibilities.

The JSON report contains every image SHA256/dimension, all 153 root console/network records and dispositions, every incomplete selector/computed layer/contrast, exact runtime checks, source file hashes and source-bound manifest/report hashes.
