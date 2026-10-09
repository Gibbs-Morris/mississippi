using System;
using System.Collections.Generic;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.BankAccountAggregate.State;
using MississippiSamples.Spring.Client.Features.DemoAccounts;
using MississippiSamples.Spring.Client.Features.DualEntitySelection;
using MississippiSamples.Spring.Client.Features.MoneyTransferSaga.State;
using MississippiSamples.Spring.Client.L0Tests.Components.Templates;
using MississippiSamples.Spring.Client.Pages;

using Moq;


namespace MississippiSamples.Spring.Client.L0Tests.Pages;

/// <summary>Verifies that separately rendered pages keep their connection disclosures paired.</summary>
public sealed class ConnectionDisclosurePairTests : BunitContext
{
    private void RegisterStore()
    {
        Mock<IInletStore> store = new(MockBehavior.Strict);
        store.Setup(current => current.GetState<DualEntitySelectionState>())
            .Returns(
                new DualEntitySelectionState
                {
                    AccountAId = "selected-a",
                    AccountBId = "selected-b",
                });
        store.Setup(current => current.GetState<DemoAccountsState>()).Returns(new DemoAccountsState());
        store.Setup(current => current.GetState<BankAccountAggregateState>()).Returns(new BankAccountAggregateState());
        store.Setup(current => current.GetState<MoneyTransferSagaState>()).Returns(new MoneyTransferSagaState());
        store.Setup(current => current.GetState<ProjectionsFeatureState>()).Returns(new ProjectionsFeatureState());
        store.Setup(current => current.GetState<SignalRConnectionState>()).Returns(new SignalRConnectionState());
        store.Setup(current => current.Subscribe(It.IsAny<Action>())).Returns(() => new EmptyStoreEventSubscription());
        store.Setup(current => current.Dispatch(It.IsAny<IAction>()));
        store.Setup(current => current.Dispose());
        Services.AddSingleton<IStore>(store.Object);
        Services.AddSingleton<IInletStore>(store.Object);
    }

    /// <summary>Verify distinct stable disclosure targets and focus restoration for both page instances.</summary>
    /// <param name="operations">Whether to render money pages instead of preparation pages.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatePagesKeepDistinctStableTargetsAndRestoreOwnFocus(
        bool operations
    )
    {
        RegisterStore();
        Type pageType = operations ? typeof(OperationsPage) : typeof(Accounts);
        using IRenderedComponent<IComponent> cut = Render(builder =>
        {
            builder.OpenComponent(0, pageType);
            builder.CloseComponent();
            builder.OpenComponent(1, pageType);
            builder.CloseComponent();
        });
        Assert.Equal(2, cut.FindAll(".spring-page-header > button").Count);
        string? firstTriggerReference = cut.FindAll(".spring-page-header > button")[0]
            .GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(firstTriggerReference));
        string? secondTriggerReference = cut.FindAll(".spring-page-header > button")[1]
            .GetAttribute("blazor:elementReference");
        Assert.False(string.IsNullOrWhiteSpace(secondTriggerReference));
        Assert.NotEqual(firstTriggerReference, secondTriggerReference);
        Assert.Empty(cut.FindAll(".spring-connection-details"));
        Assert.Empty(JSInterop.Invocations);
        cut.FindAll(".spring-page-header > button")[0].Click();
        cut.FindAll(".spring-page-header > button")[1].Click();
        IReadOnlyList<IElement> panels = cut.FindAll(".spring-connection-details");
        Assert.Equal(2, panels.Count);
        string? firstId = panels[0].GetAttribute("id");
        string? secondId = panels[1].GetAttribute("id");
        Assert.False(string.IsNullOrWhiteSpace(firstId));
        Assert.False(string.IsNullOrWhiteSpace(secondId));
        Assert.NotEqual(firstId, secondId);
        Assert.Single(cut.FindAll($"[id='{firstId}']"));
        Assert.Single(cut.FindAll($"[id='{secondId}']"));
        Assert.Equal(firstId, cut.FindAll(".spring-page-header > button")[0].GetAttribute("aria-controls"));
        Assert.Equal(secondId, cut.FindAll(".spring-page-header > button")[1].GetAttribute("aria-controls"));
        Assert.Empty(JSInterop.Invocations);
        if (operations)
        {
            foreach (IRenderedComponent<OperationsPage> owner in cut.FindComponents<OperationsPage>())
            {
                owner.Render();
            }
        }
        else
        {
            foreach (IRenderedComponent<Accounts> owner in cut.FindComponents<Accounts>())
            {
                owner.Render();
            }
        }

        Assert.Equal(firstId, cut.FindAll(".spring-connection-details")[0].GetAttribute("id"));
        Assert.Equal(secondId, cut.FindAll(".spring-connection-details")[1].GetAttribute("id"));
        Assert.Equal(firstId, cut.FindAll(".spring-page-header > button")[0].GetAttribute("aria-controls"));
        Assert.Equal(secondId, cut.FindAll(".spring-page-header > button")[1].GetAttribute("aria-controls"));
        Assert.Empty(JSInterop.Invocations);
        cut.FindAll(".spring-connection-details > header > button")[0].Click();
        Assert.False(cut.FindAll(".spring-page-header > button")[0].HasAttribute("aria-controls"));
        Assert.Equal("false", cut.FindAll(".spring-page-header > button")[0].GetAttribute("aria-expanded"));
        Assert.Equal(secondId, Assert.Single(cut.FindAll(".spring-connection-details")).GetAttribute("id"));
        Assert.Equal(secondId, cut.FindAll(".spring-page-header > button")[1].GetAttribute("aria-controls"));
        Assert.Equal("true", cut.FindAll(".spring-page-header > button")[1].GetAttribute("aria-expanded"));
        ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
        Assert.Equal(firstTriggerReference, focused.Id);
        Assert.Single(JSInterop.Invocations);
        cut.FindAll(".spring-page-header > button")[0].Click();
        Assert.Equal(firstId, cut.FindAll(".spring-connection-details")[0].GetAttribute("id"));
        Assert.Equal(firstId, cut.FindAll(".spring-page-header > button")[0].GetAttribute("aria-controls"));
        Assert.Equal(secondId, cut.FindAll(".spring-connection-details")[1].GetAttribute("id"));
        Assert.Single(JSInterop.Invocations);
        cut.FindAll(".spring-connection-details > header > button")[1].Click();
        Assert.Equal(firstId, Assert.Single(cut.FindAll(".spring-connection-details")).GetAttribute("id"));
        Assert.Equal(firstId, cut.FindAll(".spring-page-header > button")[0].GetAttribute("aria-controls"));
        Assert.Equal("true", cut.FindAll(".spring-page-header > button")[0].GetAttribute("aria-expanded"));
        Assert.False(cut.FindAll(".spring-page-header > button")[1].HasAttribute("aria-controls"));
        Assert.Equal("false", cut.FindAll(".spring-page-header > button")[1].GetAttribute("aria-expanded"));
        JSInterop.VerifyFocusAsyncInvoke(2);
        ElementReference secondFocused = Assert.IsType<ElementReference>(JSInterop.Invocations.Last().Arguments[0]);
        Assert.Equal(secondTriggerReference, secondFocused.Id);
        Assert.Equal(2, JSInterop.Invocations.Count);
        cut.FindAll(".spring-page-header > button")[1].Click();
        Assert.Equal(firstId, cut.FindAll(".spring-connection-details")[0].GetAttribute("id"));
        Assert.Equal(secondId, cut.FindAll(".spring-connection-details")[1].GetAttribute("id"));
        Assert.Equal(secondId, cut.FindAll(".spring-page-header > button")[1].GetAttribute("aria-controls"));
        Assert.Equal(2, JSInterop.Invocations.Count);
    }
}