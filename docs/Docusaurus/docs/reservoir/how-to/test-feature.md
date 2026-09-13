---
title: Test a Reservoir Feature and Effect
description: Use StoreTestHarness to verify reducers, effect emissions, explicit result application, and cancellation without rendering a UI.
sidebar_position: 3
---

# Test a Reservoir Feature and Effect

## Overview

Use `StoreTestHarness` to supply known state and actions, await an effect, and inspect its emitted actions. Assert the emitted action separately from the state produced when that action is applied.

This makes asynchronous behavior reviewable: a developer or AI assistant can show the input, expected reaction, and resulting state as distinct checks.

## When to use this

Use this guide when testing an existing Reservoir feature without a browser or live external service. The controlled effect below exists only to demonstrate the harness contract; replace it with your application's effect and test services when testing real behavior.

## Before you begin

- Use an xUnit v3 test project that references the client feature assembly and `Mississippi.Reservoir.TestHarness`.
- Allow the test assembly to access internal feature types when required by your project setup. Spring already has that test access.
- Use the state and actions from [Add a Reservoir feature](./create-feature.md), or substitute your feature consistently.

The complete example files are included in `Spring.Client.L0Tests`. To add the same source-project dependency in a Mississippi checkout, run from the repository root:

```powershell
dotnet add samples/Spring/Spring.Client.L0Tests/Spring.Client.L0Tests.csproj reference src/Reservoir.TestHarness/Reservoir.TestHarness.csproj
```

For a consumer application, reference the matching `Mississippi.Reservoir.TestHarness` package in the test project. The example uses Spring's namespace and existing xUnit imports; adapt those to your test assembly when copying the files.

## Steps

### 1. Define a Controlled Effect

Add `SelectionFollowUpEffect.cs` to your test feature folder. In Spring, the path is `samples/Spring/Spring.Client.L0Tests/Features/DualEntitySelection/SelectionFollowUpEffect.cs`. It emits a B-selection action from the A-selection state supplied after reduction. Its explicit token check makes cancellation observable in the test.

```csharp
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.DualEntitySelection;


namespace MississippiSamples.Spring.Client.L0Tests.Features.DualEntitySelection;

/// <summary>
///     Emits a follow-up selection action for controlled harness verification.
/// </summary>
internal sealed class SelectionFollowUpEffect : ActionEffectBase<SetEntityAIdAction, DualEntitySelectionState>
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<IAction> HandleAsync(
        SetEntityAIdAction action,
        DualEntitySelectionState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // This fixture deliberately derives its follow-up from the reduced state.
        _ = action;
        cancellationToken.ThrowIfCancellationRequested();
        yield return new SetEntityBIdAction(currentState.AccountAId ?? string.Empty);
    }
}
```

### 2. Create the Harness and Assertions

Add `DualEntitySelectionHarnessTests.cs` alongside the effect. The Spring path is `samples/Spring/Spring.Client.L0Tests/Features/DualEntitySelection/DualEntitySelectionHarnessTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.TestHarness;

using MississippiSamples.Spring.Client.Features.DualEntitySelection;


namespace MississippiSamples.Spring.Client.L0Tests.Features.DualEntitySelection;

/// <summary>
///     Verifies the harness recipe for effect emissions and cancellation.
/// </summary>
public sealed class DualEntitySelectionHarnessTests
{
    private static StoreTestHarness<DualEntitySelectionState> CreateHarness() =>
        StoreTestHarnessFactory.ForFeature<DualEntitySelectionState>()
            .WithReducer<SetEntityAIdAction>(DualEntitySelectionReducers.SetEntityAId)
            .WithReducer<SetEntityBIdAction>(DualEntitySelectionReducers.SetEntityBId)
            .WithEffect<SelectionFollowUpEffect>();

    /// <summary>
    ///     The scenario captures effect output until the test explicitly applies it.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task CapturedActionsAreAppliedExplicitly()
    {
        using StoreScenario<DualEntitySelectionState> scenario = CreateHarness().CreateScenario();
        scenario.Given(new SetEntityAIdAction("seed-a"), new SetEntityBIdAction("seed-b"));
        Assert.Empty(scenario.EmittedActions);
        await scenario.WhenAsync(new SetEntityAIdAction("selected-a"), TestContext.Current.CancellationToken);
        Assert.Equal("selected-a", scenario.State.AccountAId);
        Assert.Equal("seed-b", scenario.State.AccountBId);
        SetEntityBIdAction followUp = Assert.IsType<SetEntityBIdAction>(Assert.Single(scenario.EmittedActions));
        Assert.Equal("selected-a", followUp.EntityId);
        await scenario.WhenAsync(followUp, TestContext.Current.CancellationToken);
        Assert.Equal("selected-a", scenario.State.AccountBId);
        Assert.Equal(2, scenario.DispatchedActions.Count);
        Assert.Single(scenario.EmittedActions);
    }

    /// <summary>
    ///     The harness passes an explicit cancellation token to the effect.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task ExplicitCancellationReachesTheEffect()
    {
        using StoreScenario<DualEntitySelectionState> scenario = CreateHarness().CreateScenario();
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await scenario.WhenAsync(new SetEntityAIdAction("selected-a"), cancellation.Token);
        });
        Assert.Equal("selected-a", scenario.State.AccountAId);
        Assert.Empty(scenario.EmittedActions);
    }
}
```

`WithReducer` supplies the feature transitions. `WithEffect<TEffect>()` lets the scenario resolve the effect from its service provider. For injected dependencies, register a test instance with `WithService<TService>(instance)` before creating the scenario, or supply a constructed effect with `WithEffect(effect)`.

### 3. Distinguish Setup, Execution, and Results

| Operation | Harness behavior |
| --- | --- |
| `WithInitialState(state)` | Sets the starting state for new scenarios |
| `Given(actions)` | Applies setup actions through reducers |
| `GivenState(state)` | Sets a scenario's state directly |
| `WhenAsync(action, token)` | Applies reducers, then awaits matching effects |
| `EmittedActions` | Contains the actions yielded by effects |
| `DispatchedActions` | Contains actions supplied to `When`/`WhenAsync` |
| `ThenEmits<TAction>(assertion)` | Checks the first emitted action of that type |
| `ThenEmitsNothing()` | Requires an empty emitted-action collection |
| `ThenState(assertion)` | Checks the current scenario state |

In the first test, B remains `seed-b` immediately after the A action because the harness captures the follow-up. The test then passes that captured action to `WhenAsync` and verifies B changes. The live store performs that dispatch automatically; the harness makes the two assertions explicit.

Use collection assertions such as `Assert.Single` when exact count or order matters. `ThenEmits<TAction>` is an existence/type-specific assertion, not an exact emission-count check. Dispose each scenario to release its service provider.

The Spring client test project also declares `MutationSourceProject` as `../Spring.Client/Spring.Client.csproj`. The repository quality script uses this explicit project-relative target before its usual inference, so the test-harness dependency remains test support when mutation is requested. The commands below also pass `-SourceProject` explicitly.

Use sealed action types, as in this example, when testing the same action identity used by live dispatch. Harness reducer/effect matching can accept an assignable derived action through `is` and `CanHandle`, while the live store indexes typed registrations by exact runtime action type. If your feature relies on action inheritance, verify that dispatch behavior with the real store as well as the harness.

## Verify the result

Run the sample test project from the repository root:

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject samples/Spring/Spring.Client.L0Tests/Spring.Client.L0Tests.csproj -SourceProject samples/Spring/Spring.Client/Spring.Client.csproj -SkipMutation
```

Require exit code 0, `RESULT: PASS`, and executed passing tests. Confirm both `DualEntitySelectionHarnessTests` cases in the emitted TRX: one proves captured actions and explicit application; the other proves that the harness token reaches the effect.

The cancellation case also checks the already-reduced state: canceling effect enumeration leaves that transition observable.

These cases deliberately avoid timing-based waits and external services. Use controlled test doubles for your own I/O so failures describe the behavior being checked.

## Source and Contracts

- [StoreTestHarness](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.TestHarness/StoreTestHarness.cs).
- [StoreScenario](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.TestHarness/StoreScenario.cs).
- [Executable sample tests](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client.L0Tests/Features/DualEntitySelection/DualEntitySelectionHarnessTests.cs).

## Summary

Arrange known state, execute one action, inspect effect output, and apply a captured result explicitly when verifying its reducer. This separates the effect contract from the resulting state transition.

## Next Steps

- [Action effect reference](../reference/action-effects.md) for matching, lifetime, and failure handling.
- [Selector reference](../reference/selectors.md) for pure derived-value tests.
