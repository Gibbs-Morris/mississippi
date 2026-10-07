using System;
using System.Collections.Generic;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;

using MississippiSamples.Spring.Client.Features.BankAccountAggregate.State;
using MississippiSamples.Spring.Client.Features.DemoAccounts;
using MississippiSamples.Spring.Client.Features.DualEntitySelection;
using MississippiSamples.Spring.Client.L0Tests.Components.Templates;
using MississippiSamples.Spring.Client.Pages;

using Moq;


namespace MississippiSamples.Spring.Client.L0Tests.Pages;

/// <summary>
///     Verifies account selection through the rendered preparation page.
/// </summary>
public sealed class AccountsTests : BunitContext
{
    /// <summary>
    ///     Gets valid account pairs and their expected relative navigation URIs.
    /// </summary>
    public static TheoryData<string, string, string, string, Uri> CustomSelectionCases { get; } = new()
    {
        {
            "  savings a  ", "  current/b  ", "savings a", "current/b",
            new("/operations?a=savings%20a&b=current%2Fb", UriKind.Relative)
        },
        { " A+%#?& ", " B=£ ", "A+%#?&", "B=£", new("/operations?a=A%2B%25%23%3F%26&b=B%3D%C2%A3", UriKind.Relative) },
        { " same ", " same ", "same", "same", new("/operations?a=same&b=same", UriKind.Relative) },
    };

    /// <summary>
    ///     Finds the custom-selection control by its visible label.
    /// </summary>
    /// <param name="cut">The rendered account page.</param>
    /// <returns>The custom-selection button.</returns>
    private static IElement FindSelectionButton(
        IRenderedComponent<Accounts> cut
    ) =>
        cut.FindAll("button").Single(button => button.TextContent == "Use these accounts");

    /// <summary>
    ///     Supplies feature state and records the page's outgoing actions without running effects.
    /// </summary>
    /// <param name="actions">The dispatched-action collection.</param>
    /// <param name="selection">The initial account selection.</param>
    private void RegisterStore(
        List<IAction> actions,
        DualEntitySelectionState? selection = null
    )
    {
        Mock<IInletStore> store = new(MockBehavior.Strict);
        store.Setup(current => current.GetState<DualEntitySelectionState>())
            .Returns(selection ?? new DualEntitySelectionState());
        store.Setup(current => current.GetState<DemoAccountsState>()).Returns(new DemoAccountsState());
        store.Setup(current => current.GetState<BankAccountAggregateState>()).Returns(new BankAccountAggregateState());
        store.Setup(current => current.GetState<ProjectionsFeatureState>()).Returns(new ProjectionsFeatureState());
        store.Setup(current => current.GetState<SignalRConnectionState>()).Returns(new SignalRConnectionState());
        store.Setup(current => current.Subscribe(It.IsAny<Action>())).Returns(() => new EmptyStoreEventSubscription());
        store.Setup(current => current.Dispatch(It.IsAny<IAction>())).Callback<IAction>(actions.Add);
        store.Setup(current => current.Dispose());
        Services.AddSingleton<IStore>(store.Object);
        Services.AddSingleton<IInletStore>(store.Object);
    }

    /// <summary>Verify that custom IDs are trimmed, selected in order, and escaped into the navigation URI.</summary>
    /// <param name="inputA">The entered account A ID.</param>
    /// <param name="inputB">The entered account B ID.</param>
    /// <param name="expectedA">The selected account A ID.</param>
    /// <param name="expectedB">The selected account B ID.</param>
    /// <param name="expectedUri">The expected navigation URI.</param>
    [Theory]
    [MemberData(nameof(CustomSelectionCases))]
    public void CustomSelectionDispatchesTrimmedPairAndEncodedNavigation(
        string inputA,
        string inputB,
        string expectedA,
        string expectedB,
        Uri expectedUri
    )
    {
        List<IAction> actions = [];
        RegisterStore(actions);
        using IRenderedComponent<Accounts> cut = Render<Accounts>();
        cut.Find("#custom-account-a").Input(inputA);
        cut.Find("#custom-account-b").Input(inputB);
        IElement selectionButton = FindSelectionButton(cut);
        Assert.False(selectionButton.HasAttribute("disabled"));
        selectionButton.Click();
        Assert.Collection(
            actions,
            action => Assert.Equal(expectedA, Assert.IsType<SetEntityAIdAction>(action).EntityId),
            action => Assert.Equal(expectedB, Assert.IsType<SetEntityBIdAction>(action).EntityId),
            action =>
            {
                NavigateAction navigation = Assert.IsType<NavigateAction>(action);
                Assert.Equal(expectedUri.OriginalString, navigation.Uri);
                Assert.False(navigation.ForceLoad);
            });
    }

    /// <summary>Verify that an incomplete pair cannot select accounts or navigate.</summary>
    /// <param name="inputA">The entered account A ID.</param>
    /// <param name="inputB">The entered account B ID.</param>
    [Theory]
    [InlineData("", "")]
    [InlineData("", "valid-b")]
    [InlineData("   ", "valid-b")]
    [InlineData("valid-a", "")]
    [InlineData("valid-a", "   ")]
    [InlineData("   ", "   ")]
    public void IncompletePairDisablesSelection(
        string inputA,
        string inputB
    )
    {
        List<IAction> actions = [];
        RegisterStore(actions);
        using IRenderedComponent<Accounts> cut = Render<Accounts>();
        cut.Find("#custom-account-a").Input(inputA);
        cut.Find("#custom-account-b").Input(inputB);
        IElement selectionButton = FindSelectionButton(cut);
        Assert.True(selectionButton.HasAttribute("disabled"));

        // Exercise the handler's defensive check through a synthetic event.
        selectionButton.TriggerEvent("onclick", new MouseEventArgs());
        Assert.Empty(actions);
    }

    /// <summary>Verify that existing selection state prepopulates both fields without dispatching new work.</summary>
    [Fact]
    public void SelectedPairPrepopulatesInputs()
    {
        List<IAction> actions = [];
        RegisterStore(
            actions,
            new()
            {
                AccountAId = "selected-a",
                AccountBId = "selected-b",
            });
        using IRenderedComponent<Accounts> cut = Render<Accounts>();
        Assert.Equal("selected-a", cut.Find("#custom-account-a").GetAttribute("value"));
        Assert.Equal("selected-b", cut.Find("#custom-account-b").GetAttribute("value"));
        Assert.True(cut.Find("#custom-accounts").HasAttribute("open"));
        Assert.False(FindSelectionButton(cut).HasAttribute("disabled"));
        Assert.Empty(actions);
    }
}