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