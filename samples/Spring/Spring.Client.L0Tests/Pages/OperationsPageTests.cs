using System;
using System.Collections.Generic;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;

using MississippiSamples.Spring.Client.Features.BankAccountAggregate.State;
using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
using MississippiSamples.Spring.Client.Features.BankAccountLedger.Dtos;
using MississippiSamples.Spring.Client.Features.DemoAccounts;
using MississippiSamples.Spring.Client.Features.DualEntitySelection;
using MississippiSamples.Spring.Client.Features.MoneyTransferSaga.State;
using MississippiSamples.Spring.Client.L0Tests.Components.Templates;
using MississippiSamples.Spring.Client.Pages;

using Moq;


namespace MississippiSamples.Spring.Client.L0Tests.Pages;

/// <summary>
///     Verifies switching an account through the rendered money page.
/// </summary>
public sealed class OperationsPageTests : BunitContext
{
    /// <summary>
    ///     Applies selection reducers and records outgoing actions without running effects.
    /// </summary>
    /// <param name="actions">The dispatched actions.</param>
    /// <returns>The store supplying the current selection.</returns>
    private IInletStore RegisterStore(
        List<IAction> actions
    )
    {
        DualEntitySelectionState selection = new()
        {
            AccountAId = "selected-a",
            AccountBId = "selected-b",
        };
        Mock<IInletStore> store = new(MockBehavior.Strict);
        store.Setup(current => current.GetState<DualEntitySelectionState>()).Returns(() => selection);
        store.Setup(current => current.GetState<DemoAccountsState>()).Returns(new DemoAccountsState());
        store.Setup(current => current.GetState<BankAccountAggregateState>()).Returns(new BankAccountAggregateState());
        store.Setup(current => current.GetState<MoneyTransferSagaState>()).Returns(new MoneyTransferSagaState());
        store.Setup(current => current.GetState<ProjectionsFeatureState>()).Returns(new ProjectionsFeatureState());
        store.Setup(current => current.GetState<SignalRConnectionState>()).Returns(new SignalRConnectionState());
        store.Setup(current => current.Subscribe(It.IsAny<Action>())).Returns(() => new EmptyStoreEventSubscription());
        store.Setup(current => current.Dispatch(It.IsAny<IAction>()))
            .Callback<IAction>(action =>
            {
                actions.Add(action);
                selection = action switch
                {
                    SetEntityAIdAction setAccountA => DualEntitySelectionReducers.SetEntityAId(selection, setAccountA),
                    SetEntityBIdAction setAccountB => DualEntitySelectionReducers.SetEntityBId(selection, setAccountB),
                    var _ => selection,
                };
            });
        store.Setup(current => current.Dispose());
        Services.AddSingleton<IStore>(store.Object);
        Services.AddSingleton<IInletStore>(store.Object);
        return store.Object;
    }

    /// <summary>
    ///     Each Switch control clears only its own selection before navigating to custom accounts.
    /// </summary>
    /// <param name="switchAccountA">Whether the Account A control is selected.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SwitchClearsChosenSelectionBeforeNavigatingToCustomAccounts(
        bool switchAccountA
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<OperationsPage> cut = Render<OperationsPage>();
        Assert.Contains("selected-a", cut.Find("#account-a-operations-panel").TextContent, StringComparison.Ordinal);
        Assert.Contains("selected-b", cut.Find("#account-b-operations-panel").TextContent, StringComparison.Ordinal);
        actions.Clear();
        string label = switchAccountA ? "Switch Account A account" : "Switch Account B account";
        cut.Find($"button[aria-label='{label}']").Click();
        DualEntitySelectionState selection = store.GetState<DualEntitySelectionState>();
        Assert.Null(switchAccountA ? selection.AccountAId : selection.AccountBId);
        Assert.Equal(
            switchAccountA ? "selected-b" : "selected-a",
            switchAccountA ? selection.AccountBId : selection.AccountAId);
        string clearedAccountId = switchAccountA ? "selected-a" : "selected-b";
        Assert.Collection(
            actions,
            action =>
            {
                string clearedId = switchAccountA
                    ? Assert.IsType<SetEntityAIdAction>(action).EntityId
                    : Assert.IsType<SetEntityBIdAction>(action).EntityId;
                Assert.Equal(string.Empty, clearedId);
            },
            action =>
            {
                NavigateAction navigation = Assert.IsType<NavigateAction>(action);
                Assert.Equal("/accounts#custom-accounts", navigation.Uri);
                Assert.False(navigation.ForceLoad);
            },
            action => Assert.Equal(
                clearedAccountId,
                Assert.IsType<UnsubscribeFromProjectionAction<BankAccountBalanceProjectionDto>>(action).EntityId),
            action => Assert.Equal(
                clearedAccountId,
                Assert.IsType<UnsubscribeFromProjectionAction<BankAccountLedgerProjectionDto>>(action).EntityId));
    }
}