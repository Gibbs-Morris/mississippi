using System;
using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        Services.AddSingleton(store.Object);
        return store.Object;
    }

    /// <summary>Verify that only primary unmodified jump activation moves focus; native modified links retain their pair and selection.</summary>
    /// <param name="accountA">Whether the Account A jump is activated.</param>
    /// <param name="activation">The primary, non-primary or keyboard-modified activation.</param>
    [Theory]
    [InlineData(true, "primary")]
    [InlineData(false, "primary")]
    [InlineData(true, "middle")]
    [InlineData(false, "middle")]
    [InlineData(true, "secondary")]
    [InlineData(false, "secondary")]
    [InlineData(true, "control")]
    [InlineData(false, "control")]
    [InlineData(true, "meta")]
    [InlineData(false, "meta")]
    [InlineData(true, "shift")]
    [InlineData(false, "shift")]
    [InlineData(true, "alt")]
    [InlineData(false, "alt")]
    public void AccountJumpPreservesNativeModifiedActivation(
        bool accountA,
        string activation
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<OperationsPage> cut = Render<OperationsPage>();
        actions.Clear();
        Assert.Empty(JSInterop.Invocations);
        string panelId = accountA ? "account-a-operations-panel" : "account-b-operations-panel";
        string expectedUri = "http://localhost/operations?a=selected-a&b=selected-b#" + panelId;
        IElement jump = cut.Find($".spring-account-jumps a[href$='#{panelId}']");
        Assert.Equal(expectedUri, jump.GetAttribute("href"));
        string? panelReference = cut.Find("#" + panelId).GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(panelReference), cut.Find("#" + panelId).OuterHtml);
        MouseEventArgs eventArgs = activation switch
        {
            "primary" => new(),
            "middle" => new()
            {
                Button = 1,
            },
            "secondary" => new()
            {
                Button = 2,
            },
            "control" => new()
            {
                CtrlKey = true,
            },
            "meta" => new()
            {
                MetaKey = true,
            },
            "shift" => new()
            {
                ShiftKey = true,
            },
            "alt" => new()
            {
                AltKey = true,
            },
            var _ => throw new ArgumentOutOfRangeException(nameof(activation)),
        };
        jump.TriggerEvent("onclick", eventArgs);
        if (activation == "primary")
        {
            ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
            Assert.Equal(panelReference, focused.Id);
        }
        else
        {
            Assert.Empty(JSInterop.Invocations);
        }

        Assert.Empty(actions);
        Assert.Equal("selected-a", store.GetState<DualEntitySelectionState>().AccountAId);
        Assert.Equal("selected-b", store.GetState<DualEntitySelectionState>().AccountBId);
        Assert.Equal(expectedUri, jump.GetAttribute("href"));
    }

    /// <summary>Verify that each Switch control clears only its own selection before navigating to custom accounts.</summary>
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