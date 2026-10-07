using System;
using System.Collections.Generic;
using System.Linq;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;
using MississippiSamples.Spring.Client.Features.AuthProofAggregate.Actions;
using MississippiSamples.Spring.Client.Features.AuthProofAggregate.State;
using MississippiSamples.Spring.Client.Features.AuthProofRead;
using MississippiSamples.Spring.Client.Features.AuthProofSaga.Actions;
using MississippiSamples.Spring.Client.Features.AuthProofSaga.State;
using MississippiSamples.Spring.Client.Features.AuthSimulation;
using MississippiSamples.Spring.Client.L0Tests.Components.Templates;
using MississippiSamples.Spring.Client.Pages;

using Moq;


namespace MississippiSamples.Spring.Client.L0Tests.Pages;

/// <summary>
///     Verifies protected reads started by the rendered Auth Proof page.
/// </summary>
public sealed class AuthProofPageTests : BunitContext
{
    /// <summary>
    ///     Checks a fresh read's entity and immutable persona against the selected profile.
    /// </summary>
    /// <param name="action">The dispatched read.</param>
    /// <param name="entityId">The expected entity.</param>
    /// <param name="profile">The expected persona profile.</param>
    /// <param name="previousRequestId">The request this read must replace.</param>
    private static void AssertRead(
        IAction action,
        string entityId,
        SetAuthSimulationProfileAction profile,
        Guid? previousRequestId = null
    )
    {
        ReadAuthProofProjectionAction read = Assert.IsType<ReadAuthProofProjectionAction>(action);
        Assert.Equal(entityId, read.EntityId);
        Assert.NotEqual(Guid.Empty, read.RequestId);
        Assert.NotEqual(previousRequestId, read.RequestId);
        Assert.Equal(profile.Name, read.Persona.Name);
        Assert.Equal(profile.Description, read.Persona.Description);
        Assert.Equal(profile.IsAnonymous, read.Persona.IsAnonymous);
        Assert.Equal(profile.Roles, read.Persona.Roles);
        Assert.Equal(profile.Claims, read.Persona.Claims);
    }

    /// <summary>
    ///     Resolves the persona represented by a visible control.
    /// </summary>
    /// <param name="name">The control's label.</param>
    /// <returns>The corresponding production profile.</returns>
    private static SetAuthSimulationProfileAction GetProfile(
        string name
    ) =>
        name switch
        {
            "Unauthenticated" => AuthSimulationProfiles.Unauthenticated,
            "Operator Roles" => AuthSimulationProfiles.OperatorRoles,
            "AuthProof Role" => AuthSimulationProfiles.AuthProofRole,
            "AuthProof Claim" => AuthSimulationProfiles.AuthProofClaim,
            "Full Access" => AuthSimulationProfiles.FullAccess,
            var _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

    /// <summary>
    ///     Applies production persona and read reducers without running HTTP effects.
    /// </summary>
    /// <param name="actions">The outgoing actions to record.</param>
    /// <param name="initialProfile">The persona before rendering.</param>
    /// <returns>The store supplying current feature state.</returns>
    private IInletStore RegisterStore(
        List<IAction> actions,
        SetAuthSimulationProfileAction? initialProfile = null
    )
    {
        AuthSimulationState persona = AuthSimulationReducers.SetProfile(
            new(),
            initialProfile ?? AuthSimulationProfiles.FullAccess);
        AuthProofReadState read = new();
        Mock<IInletStore> store = new(MockBehavior.Strict);
        store.Setup(current => current.GetState<AuthSimulationState>()).Returns(() => persona);
        store.Setup(current => current.GetState<AuthProofReadState>()).Returns(() => read);
        store.Setup(current => current.GetState<AuthProofAggregateState>()).Returns(new AuthProofAggregateState());
        store.Setup(current => current.GetState<AuthProofSagaState>()).Returns(new AuthProofSagaState());
        store.Setup(current => current.GetState<ProjectionsFeatureState>()).Returns(new ProjectionsFeatureState());
        store.Setup(current => current.Subscribe(It.IsAny<Action>())).Returns(() => new EmptyStoreEventSubscription());
        store.Setup(current => current.Dispatch(It.IsAny<IAction>()))
            .Callback<IAction>(action =>
            {
                actions.Add(action);
                if (action is SetAuthSimulationProfileAction profile)
                {
                    persona = AuthSimulationReducers.SetProfile(persona, profile);
                    read = AuthProofReadReducers.InvalidatePersona(read, profile);
                }
                else if (action is ReadAuthProofProjectionAction request)
                {
                    read = AuthProofReadReducers.Request(read, request);
                }
                else if (action is AuthProofProjectionReadCompletedAction completed)
                {
                    read = AuthProofReadReducers.Complete(read, completed);
                }
            });
        store.Setup(current => current.Dispose());
        Services.AddSingleton<IStore>(store.Object);
        Services.AddSingleton(store.Object);
        return store.Object;
    }

    /// <summary>Verify that protected commands target the selected entity while a different draft remains unapplied.</summary>
    /// <param name="label">The protected command's visible button label.</param>
    [Theory]
    [InlineData("Record Authenticated Access")]
    [InlineData("Record Policy Access")]
    [InlineData("Record Role Access")]
    public void CommandsRetainSelectedEntityDuringDraftEditing(
        string label
    )
    {
        List<IAction> actions = [];
        RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        cut.Find("#auth-proof-entity").Input("selected-entity");
        cut.Find(".spring-auth-entity-form").Submit();
        actions.Clear();
        cut.Find("#auth-proof-entity").Input("unapplied-entity");
        Assert.Empty(actions);
        cut.FindAll("button").Single(button => button.TextContent == label).Click();
        IAction command = Assert.Single(actions);
        string entityId = label switch
        {
            "Record Authenticated Access" => Assert.IsType<RecordAuthenticatedAccessAction>(command).EntityId,
            "Record Policy Access" => Assert.IsType<RecordPolicyAccessAction>(command).EntityId,
            "Record Role Access" => Assert.IsType<RecordRoleAccessAction>(command).EntityId,
            var _ => throw new ArgumentOutOfRangeException(nameof(label)),
        };
        Assert.Equal("selected-entity", entityId);
        Assert.Equal("unapplied-entity", cut.Find("#auth-proof-entity").GetAttribute("value"));
    }

    /// <summary>
    ///     A completed empty or denied read waits for an explicit refresh rather than retrying automatically.
    /// </summary>
    /// <param name="error">The completed read's error, or null for an empty success.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("403 Forbidden")]
    public void CompletedReadWaitsForManualRefresh(
        string? error
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        Guid previousRequestId = Assert.IsType<ReadAuthProofProjectionAction>(actions[1]).RequestId;
        store.Dispatch(new AuthProofProjectionReadCompletedAction(previousRequestId, null, -1, error));
        actions.Clear();
        cut.Render();
        Assert.Empty(actions);
        Assert.Contains(error ?? "No projection data received yet", cut.Markup, StringComparison.Ordinal);
        cut.FindAll("button").Single(button => button.TextContent == "Refresh protected read").Click();
        AssertRead(Assert.Single(actions), "auth-proof", AuthSimulationProfiles.FullAccess, previousRequestId);
    }

    /// <summary>Verify that editing and blurring a draft cannot replace an active pending or completed read.</summary>
    /// <param name="completeRead">Whether the original read completed with no data.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DraftEntityEditingDoesNotReplaceActiveRead(
        bool completeRead
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        Guid requestId = Assert.IsType<ReadAuthProofProjectionAction>(actions[1]).RequestId;
        if (completeRead)
        {
            store.Dispatch(new AuthProofProjectionReadCompletedAction(requestId, null, 0, null));
            cut.Render();
        }

        AuthProofReadState retained = store.GetState<AuthProofReadState>();
        Assert.Equal(completeRead ? 0 : -1, retained.Version);
        Assert.Null(retained.Data);
        Assert.Null(retained.ErrorMessage);
        actions.Clear();
        foreach (string draft in new[] { "p", "pr", "proof-draft" })
        {
            cut.Find("#auth-proof-entity").Input(draft);
            Assert.Empty(actions);
            Assert.Equal(requestId, store.GetState<AuthProofReadState>().RequestId);
            Assert.Equal("auth-proof", store.GetState<AuthProofReadState>().EntityId);
            Assert.Same(retained, store.GetState<AuthProofReadState>());
            Assert.Equal("auth-proof", cut.Find(".spring-persona-content p > code").TextContent);
        }

        cut.Find("#auth-proof-entity").Blur();
        cut.Render();
        Assert.Empty(actions);
        Assert.Equal(requestId, store.GetState<AuthProofReadState>().RequestId);
        Assert.Equal(!completeRead, store.GetState<AuthProofReadState>().IsLoading);
        Assert.Same(retained, store.GetState<AuthProofReadState>());
    }

    /// <summary>
    ///     Verify that submitting the entity replaces its subscription and starts a read of the trimmed or default ID.
    /// </summary>
    /// <param name="input">The next entity draft.</param>
    /// <param name="expectedId">The entity that should be read.</param>
    [Theory]
    [InlineData("  proof-other  ", "proof-other")]
    [InlineData("", "auth-proof")]
    [InlineData("   ", "auth-proof")]
    public void EntityChangeReplacesSubscriptionAndStartsProtectedRead(
        string input,
        string expectedId
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        cut.Find("#auth-proof-entity").Input("previous-entity");
        cut.Find(".spring-auth-entity-form").Submit();
        Guid? previousRequestId = store.GetState<AuthProofReadState>().RequestId;
        actions.Clear();
        cut.Find("#auth-proof-entity").Input(input);
        Assert.Empty(actions);
        cut.Find(".spring-auth-entity-form").Submit();
        Assert.Collection(
            actions,
            action => Assert.Equal(
                "previous-entity",
                Assert.IsType<UnsubscribeFromProjectionAction<AuthProofProjectionDto>>(action).EntityId),
            action => Assert.Equal(
                expectedId,
                Assert.IsType<SubscribeToProjectionAction<AuthProofProjectionDto>>(action).EntityId),
            action => AssertRead(action, expectedId, AuthSimulationProfiles.FullAccess, previousRequestId));
    }

    /// <summary>
    ///     The initial render subscribes and reads the default entity with the selected persona.
    /// </summary>
    [Fact]
    public void InitialRenderStartsProtectedRead()
    {
        List<IAction> actions = [];
        RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        Assert.Collection(
            actions,
            action => Assert.Equal(
                "auth-proof",
                Assert.IsType<SubscribeToProjectionAction<AuthProofProjectionDto>>(action).EntityId),
            action => AssertRead(action, "auth-proof", AuthSimulationProfiles.FullAccess));
    }

    /// <summary>
    ///     Every persona selection, including reselecting the active profile, starts a fresh protected read.
    /// </summary>
    /// <param name="name">The selected persona label.</param>
    /// <param name="alreadySelected">Whether the persona was already active.</param>
    [Theory]
    [InlineData("Unauthenticated", false)]
    [InlineData("Operator Roles", false)]
    [InlineData("AuthProof Role", false)]
    [InlineData("AuthProof Claim", false)]
    [InlineData("Full Access", false)]
    [InlineData("Unauthenticated", true)]
    [InlineData("Operator Roles", true)]
    [InlineData("AuthProof Role", true)]
    [InlineData("AuthProof Claim", true)]
    [InlineData("Full Access", true)]
    public void PersonaSelectionStartsFreshProtectedRead(
        string name,
        bool alreadySelected
    )
    {
        SetAuthSimulationProfileAction expectedProfile = GetProfile(name);
        SetAuthSimulationProfileAction differentProfile = name == "Full Access"
            ? AuthSimulationProfiles.Unauthenticated
            : AuthSimulationProfiles.FullAccess;
        SetAuthSimulationProfileAction initialProfile = alreadySelected ? expectedProfile : differentProfile;
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions, initialProfile);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        cut.Find("#auth-proof-entity").Input("persona-entity");
        cut.Find(".spring-auth-entity-form").Submit();
        Guid? previousRequestId = store.GetState<AuthProofReadState>().RequestId;
        actions.Clear();
        cut.Find("#auth-proof-entity").Input("unapplied-persona-draft");
        Assert.Empty(actions);
        cut.FindAll(".spring-personas button").Single(button => button.TextContent == name).Click();
        Assert.Collection(
            actions,
            action => Assert.Equal(expectedProfile, Assert.IsType<SetAuthSimulationProfileAction>(action)),
            action => AssertRead(action, "persona-entity", expectedProfile, previousRequestId));
        Assert.Equal(
            "true",
            cut.FindAll(".spring-personas button")
                .Single(button => button.TextContent == name)
                .GetAttribute("aria-pressed"));
    }

    /// <summary>Verify that a saga start carries the selected marker while a different entity draft remains unapplied.</summary>
    [Fact]
    public void SagaStartRetainsSelectedMarkerDuringDraftEditing()
    {
        List<IAction> actions = [];
        RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        cut.Find("#auth-proof-entity").Input("selected-entity");
        cut.Find(".spring-auth-entity-form").Submit();
        actions.Clear();
        cut.Find("#auth-proof-entity").Input("unapplied-entity");
        Assert.Empty(actions);
        cut.FindAll("button").Single(button => button.TextContent == "Start AuthProof Saga").Click();
        StartAuthProofSagaAction action = Assert.IsType<StartAuthProofSagaAction>(Assert.Single(actions));
        Assert.Equal("selected-entity", action.Marker);
        Assert.NotEqual(Guid.Empty, action.SagaId);
        Assert.Equal(action.SagaId.ToString(), action.EntityId);
        Assert.NotNull(action.CorrelationId);
        Assert.Matches("^[0-9a-f]{32}$", action.CorrelationId);
        Assert.Equal("unapplied-entity", cut.Find("#auth-proof-entity").GetAttribute("value"));
    }

    /// <summary>Verify that submitting the current normalized entity does not replace its read or subscription.</summary>
    /// <param name="draft">The unchanged entity or its trimmed/default equivalent.</param>
    [Theory]
    [InlineData("auth-proof")]
    [InlineData("  auth-proof  ")]
    [InlineData("")]
    [InlineData("   ")]
    public void SameNormalizedEntitySubmissionDoesNotRepeatRead(
        string draft
    )
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        Guid? requestId = store.GetState<AuthProofReadState>().RequestId;
        actions.Clear();
        cut.Find("#auth-proof-entity").Input(draft);
        Assert.Empty(actions);
        cut.Find(".spring-auth-entity-form").Submit();
        Assert.Empty(actions);
        Assert.Equal(requestId, store.GetState<AuthProofReadState>().RequestId);
        Assert.Equal("auth-proof", cut.Find("#auth-proof-entity").GetAttribute("value"));
    }

    /// <summary>
    ///     Rendering an unchanged pending request does not create another read.
    /// </summary>
    [Fact]
    public void UnchangedRenderDoesNotRepeatProtectedRead()
    {
        List<IAction> actions = [];
        IInletStore store = RegisterStore(actions);
        using IRenderedComponent<AuthProofPage> cut = Render<AuthProofPage>();
        Assert.True(store.GetState<AuthProofReadState>().IsLoading);
        actions.Clear();
        cut.Render();
        Assert.Empty(actions);
        Assert.True(
            cut.FindAll("button")
                .Single(button => button.TextContent == "Refresh protected read")
                .HasAttribute("disabled"));
    }
}