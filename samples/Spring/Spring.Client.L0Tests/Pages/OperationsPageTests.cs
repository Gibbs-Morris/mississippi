using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    /// <summary>Checks exact projection action types and IDs, including duplicate or unexpected actions.</summary>
    /// <param name="actions">The actions emitted by the page.</param>
    /// <param name="operation">The expected subscribe or unsubscribe operation.</param>
    /// <param name="first">The first expected unique account ID.</param>
    /// <param name="second">The second expected unique account ID.</param>
    private static void AssertAccountSubscriptionActions(
        List<IAction> actions,
        string operation,
        string? first,
        string? second
    )
    {
        List<string> expected = [];
        if (first is not null)
        {
            expected.Add(operation + "-balance:" + first);
            expected.Add(operation + "-ledger:" + first);
        }

        if (second is not null)
        {
            expected.Add(operation + "-balance:" + second);
            expected.Add(operation + "-ledger:" + second);
        }

        Assert.Equal(expected.OrderBy(value => value, StringComparer.Ordinal), DescribeSubscriptionActions(actions));
    }

    /// <summary>Describes every outgoing action without discarding unexpected types or repeated IDs.</summary>
    /// <param name="actions">The observed actions.</param>
    /// <returns>The ordered projection action descriptions.</returns>
    private static IEnumerable<string> DescribeSubscriptionActions(
        IEnumerable<IAction> actions
    ) =>
        actions.Select(action => action switch
            {
                SubscribeToProjectionAction<BankAccountBalanceProjectionDto> balance => "subscribe-balance:" +
                    balance.EntityId,
                SubscribeToProjectionAction<BankAccountLedgerProjectionDto> ledger => "subscribe-ledger:" +
                    ledger.EntityId,
                UnsubscribeFromProjectionAction<BankAccountBalanceProjectionDto> balance => "unsubscribe-balance:" +
                    balance.EntityId,
                UnsubscribeFromProjectionAction<BankAccountLedgerProjectionDto> ledger => "unsubscribe-ledger:" +
                    ledger.EntityId,
                var _ => throw new InvalidOperationException("Unexpected action: " + action.GetType().Name),
            })
            .OrderBy(value => value, StringComparer.Ordinal);

    /// <summary>
    ///     Applies selection reducers and records outgoing actions without running effects.
    /// </summary>
    /// <param name="actions">The dispatched actions.</param>
    /// <param name="initialSelection">The initial selected pair, or the default distinct pair.</param>
    /// <returns>The store supplying the current selection.</returns>
    private IInletStore RegisterStore(
        List<IAction> actions,
        DualEntitySelectionState? initialSelection = null
    )
    {
        DualEntitySelectionState selection = initialSelection ??
                                             new()
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

    /// <summary>
    ///     Verify that only primary unmodified jump activation moves focus; native modified links retain their pair and
    ///     selection.
    /// </summary>
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

    /// <summary>Verify that direct shared/query pairs normalize equal IDs and subscribe once to each projection.</summary>
    /// <param name="queryA">The query-string account A ID.</param>
    /// <param name="queryB">The query-string account B ID.</param>
    [Theory]
    [InlineData("shared", "shared")]
    [InlineData(" shared ", " shared ")]
    public void EqualQueryPairSelectsOneSharedProjectionSubscription(
        string queryA,
        string queryB
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/operations?a=" + Uri.EscapeDataString(queryA) + "&b=" + Uri.EscapeDataString(queryB));
        using IRenderedComponent<OperationsPage> cut = Render<OperationsPage>();
        Assert.Collection(
            actions,
            action => Assert.Equal("shared", Assert.IsType<SetEntityAIdAction>(action).EntityId),
            action => Assert.Equal("shared", Assert.IsType<SetEntityBIdAction>(action).EntityId),
            action => Assert.Equal(
                "shared",
                Assert.IsType<SubscribeToProjectionAction<BankAccountBalanceProjectionDto>>(action).EntityId),
            action => Assert.Equal(
                "shared",
                Assert.IsType<SubscribeToProjectionAction<BankAccountLedgerProjectionDto>>(action).EntityId));
        Assert.Equal("shared", store.GetState<DualEntitySelectionState>().AccountAId);
        Assert.Equal("shared", store.GetState<DualEntitySelectionState>().AccountBId);
        Assert.Equal("shared", cut.Find("#account-a-operations-panel h2 code").TextContent);
        Assert.Equal("shared", cut.Find("#account-b-operations-panel h2 code").TextContent);
    }

    /// <summary>Verify that panel changes release only removed IDs and subscribe only newly selected IDs.</summary>
    /// <param name="initialA">The initial account A ID.</param>
    /// <param name="initialB">The initial account B ID.</param>
    /// <param name="nextA">The next account A ID.</param>
    /// <param name="nextB">The next account B ID.</param>
    /// <param name="removed">The expected removed unique account ID, if any.</param>
    /// <param name="added">The expected added unique account ID, if any.</param>
    [Theory]
    [InlineData("shared", "shared", "shared", "new", null, "new")]
    [InlineData("shared", "shared", "new", "shared", null, "new")]
    [InlineData("first", "second", "first", "first", "second", null)]
    [InlineData("first", "second", "second", "second", "first", null)]
    [InlineData("first", "second", "second", "first", null, null)]
    [InlineData("shared", "shared", "shared", null, null, null)]
    [InlineData("shared", "shared", null, "shared", null, null)]
    [InlineData("shared", "shared", null, null, "shared", null)]
    [InlineData("first", "second", "third", "second", "first", "third")]
    public void PairTransitionRetainsSharedProjectionSubscriptions(
        string initialA,
        string initialB,
        string? nextA,
        string? nextB,
        string? removed,
        string? added
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(
            actions,
            new()
            {
                AccountAId = initialA,
                AccountBId = initialB,
            });
        using IRenderedComponent<OperationsPage> cut = Render<OperationsPage>();
        store.Dispatch(new SetEntityAIdAction(nextA ?? string.Empty));
        store.Dispatch(new SetEntityBIdAction(nextB ?? string.Empty));
        actions.Clear();
        cut.Render();
        List<string> expected = [];
        if (removed is not null)
        {
            expected.Add("unsubscribe-balance:" + removed);
            expected.Add("unsubscribe-ledger:" + removed);
        }

        if (added is not null)
        {
            expected.Add("subscribe-balance:" + added);
            expected.Add("subscribe-ledger:" + added);
        }

        Assert.Equal(expected.OrderBy(value => value, StringComparer.Ordinal), DescribeSubscriptionActions(actions));
        Assert.Equal(nextA, store.GetState<DualEntitySelectionState>().AccountAId);
        Assert.Equal(nextB, store.GetState<DualEntitySelectionState>().AccountBId);
        actions.Clear();
        cut.Render();
        Assert.Empty(actions);
    }

    /// <summary>Verify exact unique account subscriptions, stable rerenders and one release per projection.</summary>
    /// <param name="accountA">The initially selected account A ID.</param>
    /// <param name="accountB">The initially selected account B ID.</param>
    /// <param name="expectedFirst">The first expected unique account ID, if any.</param>
    /// <param name="expectedSecond">The second expected unique account ID, if any.</param>
    /// <returns>The asynchronous component-disposal check.</returns>
    [Theory]
    [InlineData("shared", "shared", "shared", null)]
    [InlineData("first", "second", "first", "second")]
    [InlineData("Shared", "shared", "Shared", "shared")]
    [InlineData("only", null, "only", null)]
    [InlineData(null, "only", "only", null)]
    [InlineData(null, null, null, null)]
    [InlineData("  ", "", null, null)]
    public async Task SelectedPairOwnsUniqueSubscriptionsThroughDisposal(
        string? accountA,
        string? accountB,
        string? expectedFirst,
        string? expectedSecond
    )
    {
        List<IAction> actions = [];
        RegisterStore(
            actions,
            new()
            {
                AccountAId = accountA,
                AccountBId = accountB,
            });
        using (IRenderedComponent<OperationsPage> cut = Render<OperationsPage>())
        {
            AssertAccountSubscriptionActions(actions, "subscribe", expectedFirst, expectedSecond);
            actions.Clear();
            cut.Render();
            Assert.Empty(actions);
        }

        await DisposeComponentsAsync();
        AssertAccountSubscriptionActions(actions, "unsubscribe", expectedFirst, expectedSecond);
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