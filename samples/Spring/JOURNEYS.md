# Spring capability and journey map

This reference maps Spring's implemented capabilities to tasks, prerequisites, visible outcomes and verification.
It applies to the local demo, not a production banking or identity service.

## Banking and live reads

| Capability | Task and prerequisite | Outcome and limits | Source and verification |
| --- | --- | --- | --- |
| First run | Start here → Prepare accounts → initialize the demo pair. Keep Full Access selected when local auth is on. | Ada and Grace have £500 each after their opening commands and projections complete. Selection is immediate; opening can still fail. | [Setup actions](Spring.Client/Pages/Accounts.razor.cs), [first-run and shared-view L3 test](Spring.L3Tests/FlagshipJourneysTests.cs), [banking smoke](Spring.L3Tests/Smoke/BankAccountSmokeTests.cs). |
| Custom accounts and switching | Expand Use your own account IDs, enter two existing or new IDs, then select them. Switch on an account panel expands selection and retains the other account ID. The demo shortcut always opens its displayed pair. | New IDs expose an Open Account form. Names are required; the initial deposit cannot be negative. Existing IDs load their projections. IDs confer no access rights. | [Selection and opening](Spring.Client/Pages/Accounts.razor.cs), [domain handlers](Spring.Domain/Aggregates/BankAccount/Handlers), [independent panels L3](Spring.L3Tests/BankAccountE2ETests.cs) and [account-switch regression](Spring.L3Tests/FlagshipJourneysTests.cs). |
| Deposit | An open account; a positive amount in the Deposit form. | An accepted command is followed by an increased live balance and a Deposit ledger entry. Domain errors are shown with the failed request and server's reason. | [Deposit handler](Spring.Domain/Aggregates/BankAccount/Handlers/DepositFundsHandler.cs), [API contracts](Spring.L2Tests/BankAccountIntegrationTests.cs), [L3 deposit and first-run tests](Spring.L3Tests/FlagshipJourneysTests.cs). |
| Withdrawal | Expand Withdraw money on an open account; enter a positive amount no greater than its available balance. | A successful request reduces the balance and adds a Withdrawal entry. Insufficient funds leave the balance and ledger unchanged. Notification side effects are simulated in server logs. | [Withdrawal handler and effects](Spring.Domain/Aggregates/BankAccount), [L3 withdrawal](Spring.L3Tests/BankAccountE2ETests.cs), [rejected withdrawal regression](Spring.L3Tests/FlagshipJourneysTests.cs). |
| Live balances and ledgers | Move money selects two IDs and subscribes to both account projections. | Balance versions and connection state are visible. Ledger retains the latest 20 deposit/withdrawal entries, newest first. The opening deposit appears in the balance, not as a ledger transaction. This UI displays the implemented GBP amounts; it has no currency-conversion control. | [Projection definitions and reducers](Spring.Domain/Projections), [page subscriptions](Spring.Client/Pages/OperationsPage.razor.cs), [second-browser and retention regressions](Spring.L3Tests/FlagshipJourneysTests.cs). |
| Shared links | Expand Open this pair in another browser, or Share or reopen the demo pair after setup. Use the same running server. | The `/operations?a=…&b=…` URL selects trimmed account IDs. Another browser has independent client state and sees the same server projections. The link neither exports data nor grants permission. | [URL selection and draft lifetime](Spring.Client/Pages/OperationsPage.razor.cs), [independent panels L3](Spring.L3Tests/BankAccountE2ETests.cs), [second-browser L3](Spring.L3Tests/FlagshipJourneysTests.cs). |
| Quick actions and bursts | Expand Quick actions & command bursts on an open account. | Preserve +/−£100 once, 20×£5 (£100 total), and 200×£10 (£2,000 total), for either account. These are independent commands without retry. Withdrawals can fail individually. Client history retains the latest 200 requests; the ledger retains 20 entries. | [Existing dispatch loops](Spring.Client/Pages/OperationsPage.razor.cs), [all six controls and exact totals L3](Spring.L3Tests/FlagshipJourneysTests.cs). |

## Transfers and investigations

| Capability | Task and prerequisite | Outcome and limits | Source and verification |
| --- | --- | --- | --- |
| Transfer between accounts | Expand Transfer to the other account. The source must be open; destination follows the other selected ID. Use distinct IDs and a positive amount. | Start-request acceptance and workflow completion are separate. The saga withdraws at step 0, then deposits at step 1. Completed phase, timestamps and both balance/ledger changes provide the outcome. | [Transfer steps](Spring.Domain/Aggregates/MoneyTransferSaga/Steps), [live status projection](Spring.Domain/Projections/MoneyTransferStatus), [completed-transfer L3](Spring.L3Tests/BankAccountE2ETests.cs). |
| Transfer failure and compensation | A destination that has not been opened causes its deposit step to fail after a valid source withdrawal. | The defined source compensation deposits the amount back. Compensated phase, original error and reversing ledger entry are visible. Compensated can also mean no forward step completed; no reversing deposit is required in that case. Check both accounts before another transfer. Failed compensation is reported as Failed; the UI offers no recovery or automatic retry control. | [Compensatable source step](Spring.Domain/Aggregates/MoneyTransferSaga/Steps/WithdrawFromSourceStep.cs), [runtime orchestration](../../src/DomainModeling.Runtime/SagaOrchestrationEffect.cs), [compensation L3 regression](Spring.L3Tests/FlagshipJourneysTests.cs). |
| Investigation queue | Deposit an amount strictly greater than £10,000, then open Investigate or View Investigations. | A real aggregate effect flags the deposit. The queue projection retains the latest 30 entries with account ID, amount and timestamps. Loading, unavailable, failed-read and loaded-empty states are distinct. This is a queue demonstration; it has no case assignment or resolution workflow. | [Threshold effect](Spring.Domain/Aggregates/BankAccount/Effects/HighValueTransactionEffect.cs), [queue projection](Spring.Domain/Projections/FlaggedTransactions), [high-value L3 regression](Spring.L3Tests/FlagshipJourneysTests.cs). |

## Auth Proof and personas

Launch with `pwsh ./run-spring.ps1 -LocalAuth On` and choose Test access.
Protected Auth Proof endpoints return 401 with LocalAuth Off regardless of the browser persona.
The page does not detect the server's launch mode.

The table gives expected HTTP outcomes with local auth on.
Projection reads require the claim policy; the saga start requires the Auth Proof role.

| Persona | Authenticated command | Claim command / projection read | Role command / saga start |
| --- | --- | --- | --- |
| Unauthenticated | 401 | 401 | 401 |
| Operator Roles | 200 | 403 | 403 |
| AuthProof Role | 200 | 403 | 200 |
| AuthProof Claim | 200 | 200 | 403 |
| Full Access | 200 | 200 | 200 |

- Select a persona, send one of the three protected commands and compare its actual response with the expected access. 200 means allowed; 401 means unauthenticated; 403 means authenticated but unauthorized.
- The read projection counts authenticated-access events only. Role and claim commands do not increment that count. Changing persona refreshes the HTTP read; choose Refresh protected read after accepted commands to verify the count. Reservoir correlates each HTTP result with its entity, persona and request ID so an older response cannot replace the current selection's result. Live subscription access is checked separately and can be denied even when a persona permits HTTP reads. A rejected read is separate from command execution.
- Start AuthProof Saga exercises role authorization on a saga with one no-op step. Its client response reports start acceptance, not the final workflow phase.
- Choose an entity ID to isolate the experiment; blank uses `auth-proof`. Command histories retain earlier personas and entities.
- Expand Inspect raw projection and client state snapshots for the existing Reservoir diagnostics. Cached projection data can remain after a denied read; assess current access from the live-read result.
- Personas also affect banking requests. Restore Full Access before returning to banking. All four authenticated profiles retain the banking and transfer operator roles.

Sources: [persona profiles](Spring.Client/Features/AuthSimulation/AuthSimulationProfiles.cs), [HTTP header adapter](Spring.Client/AuthSimulation/AuthSimulationHeadersHandler.cs), [registered projection reducers](Spring.Client/Features/ProjectionsFeatureRegistration.cs), [correlated protected HTTP read](Spring.Client/Features/AuthProofRead), [read-ordering regression](Spring.Client.L0Tests/Features/AuthProofRead/AuthProofReadTests.cs), [registration regression](Spring.Client.L0Tests/Features/ProjectionsFeatureRegistrationTests.cs), [gateway configuration](Spring.Gateway/Program.cs), [Auth Proof domain](Spring.Domain/Aggregates/AuthProof), [authorization L2 matrix](Spring.L2Tests/AuthProofAuthorizationIntegrationTests.cs) and [all five personas L3](Spring.L3Tests/FlagshipJourneysTests.cs).

## Connection, themes and developer tools

| Capability | Task and prerequisite | Outcome and limits | Source and verification |
| --- | --- | --- | --- |
| Connection diagnostics | Choose Connection status on Prepare accounts or Move money. | Nonmodal details show the actual SignalR state, connection ID, reconnect count, timestamps and error. Close returns focus to the trigger. Connecting/reconnecting is an inline notice; reconnect is available after disconnection. Cached reads may be stale. A connected transport is not proof of projection catch-up. | [Connection components](Spring.Client/Components/Organisms), [SignalR integration](../../src/Inlet.Client/SignalRConnection), component tests and [browser evidence](Spring.L3Tests/Screenshots/flagship/README.md). |
| Themes | Expand Appearance in the shell and select Dark, Light or High Contrast. | The existing Reservoir theme preference drives the Refraction provider. Controls expose their selected state. | [Shell](Spring.Client/Components/Templates/SpringShell), [theme feature](Spring.Client/Features/ThemePreferences), existing L3 theme checks and [browser evidence](Spring.L3Tests/Screenshots/flagship/README.md). |
| API reference and OpenAPI | Expand Developer tools in the footer; open API reference or OpenAPI document. | Scalar loads the real generated API document. These are gateway development tools. | [Gateway endpoints](Spring.Gateway/Program.cs), [API L2](Spring.L2Tests/ApiDocumentationIntegrationTests.cs), [Scalar L3](Spring.L3Tests/ApiDocumentationE2ETests.cs). |
| MCP tools | Connect an MCP client to the development gateway's `/mcp` endpoint. | Existing generated domain tools and `ping` remain available. This endpoint is not a browser page. UI redesign does not change MCP registration or authorization. | [Gateway MCP setup](Spring.Gateway/Program.cs). |
| Reservoir Redux DevTools | Use a compatible browser extension while running the client. | Inspect dispatched actions and client state through the existing integration. Histories shown in the UI are real generated state. | [Client registration](Spring.Client/Program.cs). |
| Aspire diagnostics | Open the dashboard URL printed by the launcher. | Inspect actual resources, logs and traces. Do not copy its private login token into shared evidence. | [Launcher](../../run-spring.ps1), [AppHost](Spring.AppHost/Program.cs). |

## UI states and architecture

The task UI exposes empty/unavailable reads, loading reads, requests awaiting responses, accepted responses and failed requests, invalid amount drafts, permission errors, pending saga phases and final saga outcomes.
It does not manufacture projection progress, retries, recovery or account-scoped command history.
The generated banking history is shared across accounts, retains the latest 200 requests and contains request IDs, not entity IDs or projection watermarks. Accepted/failed counts describe retained client results, not all requests since launch or authoritative server outcomes. A network error, cancellation or lost response can follow a server commit; inspect the real error and live projection before sending another request. The UI does not retry automatically.

Pages select Reservoir state and dispatch the existing generated actions.
Presentational components receive state through parameters and raise callbacks.
Refraction Pane, InputField, TelemetryStrip, themes and semantic tokens remain the presentation foundation.
Domain handlers, storage, runtime orchestration, generated routes and authorization rules remain unchanged.

See the [first-run guide](README.md), [test commands and evidence](TESTING.md) and [phone/desktop captures](Spring.L3Tests/Screenshots/flagship/README.md).

Spring's shared layout, type, radius and control-size values use application-owned `--spring-*` primitives in `spring.css`.
Refraction's existing semantic colors and focus tokens remain the theme contract.
The temporary hand-authored scope is recorded in [#405's migration ledger](https://github.com/Gibbs-Morris/mississippi/issues/405) as `spring-flagship-token-migration` until emitted semantic spacing/type/size properties are available.
