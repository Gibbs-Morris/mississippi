using MississippiSamples.Spring.Client.Features.DualEntitySelection;
using MississippiSamples.Spring.Client.Features.DualEntitySelection.Selectors;


namespace MississippiSamples.Spring.Client.L0Tests.Features.DualEntitySelection;

/// <summary>
///     Verifies the account-selection transitions used by the Reservoir guide.
/// </summary>
public sealed class DualEntitySelectionTests
{
    /// <summary>
    ///     Clearing a slot preserves the other slot and the original state.
    /// </summary>
    /// <param name="isClearingA">Whether the action clears slot A.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClearingOneSlotPreservesTheOther(
        bool isClearingA
    )
    {
        DualEntitySelectionState original = new()
        {
            AccountAId = "account-a",
            AccountBId = "account-b",
        };
        DualEntitySelectionState cleared = isClearingA
            ? DualEntitySelectionReducers.SetEntityAId(original, new(string.Empty))
            : DualEntitySelectionReducers.SetEntityBId(original, new(string.Empty));
        Assert.Equal(isClearingA ? null : "account-a", cleared.AccountAId);
        Assert.Equal(isClearingA ? "account-b" : null, cleared.AccountBId);
        Assert.False(DualEntitySelectionSelectors.HasAccountPair(cleared));
        Assert.Equal("account-a", original.AccountAId);
        Assert.Equal("account-b", original.AccountBId);
        Assert.NotSame(original, cleared);
    }

    /// <summary>
    ///     The presence selector checks nonblank IDs rather than transfer eligibility.
    /// </summary>
    /// <param name="accountAId">The ID stored in slot A.</param>
    /// <param name="accountBId">The ID stored in slot B.</param>
    /// <param name="hasPair">The expected presence result.</param>
    [Theory]
    [InlineData(" ", "account-b", false)]
    [InlineData("account-a", " ", false)]
    [InlineData("account-a", "account-a", true)]
    public void PresenceSelectorHasTheDocumentedMeaning(
        string accountAId,
        string accountBId,
        bool hasPair
    )
    {
        DualEntitySelectionState state = DualEntitySelectionReducers.SetEntityAId(new(), new(accountAId));
        state = DualEntitySelectionReducers.SetEntityBId(state, new(accountBId));
        Assert.Equal(accountAId, state.AccountAId);
        Assert.Equal(accountBId, state.AccountBId);
        Assert.Equal(hasPair, DualEntitySelectionSelectors.HasAccountPair(state));
    }

    /// <summary>
    ///     The guide's set-A, set-B, and clear-A sequence preserves immutable prior states.
    /// </summary>
    [Fact]
    public void SelectionSequenceMatchesTheGuide()
    {
        DualEntitySelectionState initial = new();
        DualEntitySelectionState selectedA = DualEntitySelectionReducers.SetEntityAId(initial, new("account-a"));
        Assert.Equal("account-a", DualEntitySelectionSelectors.GetAccountAId(selectedA));
        Assert.Null(DualEntitySelectionSelectors.GetAccountBId(selectedA));
        Assert.False(DualEntitySelectionSelectors.HasAccountPair(selectedA));
        Assert.Null(initial.AccountAId);
        Assert.Null(initial.AccountBId);
        DualEntitySelectionState pair = DualEntitySelectionReducers.SetEntityBId(selectedA, new("account-b"));
        Assert.Equal("account-a", pair.AccountAId);
        Assert.Equal("account-b", pair.AccountBId);
        Assert.True(DualEntitySelectionSelectors.HasAccountPair(pair));
        Assert.Null(selectedA.AccountBId);
        DualEntitySelectionState cleared = DualEntitySelectionReducers.SetEntityAId(pair, new(string.Empty));
        Assert.Null(cleared.AccountAId);
        Assert.Equal("account-b", cleared.AccountBId);
        Assert.False(DualEntitySelectionSelectors.HasAccountPair(cleared));
        Assert.Equal("account-a", pair.AccountAId);
    }
}