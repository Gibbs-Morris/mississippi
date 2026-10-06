# Try the Spring demo

Run Spring, make a deposit and verify its outcome in two live account views.

## What you will achieve

You will open two fictional bank accounts, change one balance and see an event appear in its ledger.
The client uses Blazor, Refraction, Reservoir and Inlet; the server runs the real domain through Orleans and the configured storage providers.

## Prerequisites

Use the SDK pinned by [global.json](../../global.json), PowerShell 7 and a running Docker Linux daemon.
From the repository root, check readiness:

```powershell
pwsh ./test-spring.ps1 -Doctor
```

A `READY` result checks prerequisites. It does not execute or pass tests.
See [Spring validation and diagnostics](../../README.md#validate-spring-after-a-change) if readiness fails.

## Run the sample

From the repository root:

```powershell
pwsh ./run-spring.ps1 -LocalAuth On
```

Keep that terminal running and open the gateway URL printed by the launcher.
The flag enables the sample's local, header-based authentication simulation, including Auth Proof.
It is a development demo, not production identity.

## Verify it works

1. On **Start here**, choose **Start with two accounts**.
2. Choose **Initialize demo accounts**, then **Go to Operations**.
3. Wait for Ada Lovelace and Grace Hopper to each show **£500.00**. A selected ID or an accepted command response alone is not this result.
4. In Account A, enter **25** under **Deposit** and choose **Deposit £**.
5. Verify Account A shows **£525.00**, Account B remains **£500.00**, and Account A's ledger contains **Deposit · £25.00**.
6. Expand **Open this pair in another browser**. Open that link in a second browser connected to this running server.
7. Deposit another **25** in the first browser. Verify **£550.00** and the new ledger entry in both browsers.

If a request fails, read **Banking responses**. A lost reply can follow a server commit; check the live balance and ledger before sending the request again. If a live read fails or the connection drops, read its separate error and connection details.
The displayed balance can remain stale while disconnected; do not treat command acceptance as projection catch-up.

## What happened

The generated action traveled through Reservoir and Inlet to the bank account command handler.
An accepted deposit produced an event. Separate balance and ledger projections consumed that event, and Inlet delivered their updates to the browser.
The UI reports those command responses and projected outcomes separately.

## Summary

The money is fictional; the commands, domain validation, events and live projection updates are real.
Stop the interactive app with **Ctrl+C** before running validation scripts in this worktree.

## Next steps

Use the [capability and journey map](JOURNEYS.md) to explore withdrawals, transfers and compensation, investigations, personas, bursts, themes and developer tools.
Use the [test guide](TESTING.md) to validate a change and inspect its evidence.
