using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Effects;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests.BuiltIn.Navigation.Effects;

/// <summary>
///     Unit tests for <see cref="NavigationEffect" />.
/// </summary>
public sealed class NavigationEffectTests
{
    private static Mock<IJSRuntime> CreateMockJSRuntime()
    {
        Mock<IJSRuntime> mockJs = new();
        // InvokeVoidAsync has no return value, just need to set up the method
        return mockJs;
    }

    private static async Task<List<IAction>> CollectEmittedActionsAsync(
        NavigationEffect effect,
        IAction action,
        NavigationState state
    )
    {
        List<IAction> actions = [];
        await foreach (IAction emitted in effect.HandleAsync(action, state, CancellationToken.None))
        {
            actions.Add(emitted);
        }

        return actions;
    }

    private static async Task ConsumeEffectAsync(
        NavigationEffect effect,
        IAction action,
        NavigationState state
    )
    {
        await foreach (IAction emitted in effect.HandleAsync(action, state, CancellationToken.None))
        {
            _ = emitted; // consume
        }
    }

    private static NavigationState CreateDefaultState() =>
        new()
        {
            CurrentUri = "https://example.com/",
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 0,
        };

    /// <summary>
    ///     Verifies that CanHandle returns false for LocationChangedAction (handled by reducer).
    /// </summary>
    [Fact]
    public void CanHandleLocationChangedActionReturnsFalse()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        LocationChangedAction action = new("https://example.com", false);

        // Act
        bool result = effect.CanHandle(action);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for NavigateAction.
    /// </summary>
    [Fact]
    public void CanHandleNavigateActionReturnsTrue()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        NavigateAction action = new("https://example.com");

        // Act
        bool result = effect.CanHandle(action);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for OpenExternalLinkAction.
    /// </summary>
    [Fact]
    public void CanHandleOpenExternalLinkActionReturnsTrue()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        OpenExternalLinkAction action = new("https://external.com");

        // Act
        bool result = effect.CanHandle(action);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for ReplaceRouteAction.
    /// </summary>
    [Fact]
    public void CanHandleReplaceRouteActionReturnsTrue()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        ReplaceRouteAction action = new("https://example.com");

        // Act
        bool result = effect.CanHandle(action);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for ScrollToAnchorAction.
    /// </summary>
    [Fact]
    public void CanHandleScrollToAnchorActionReturnsTrue()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        ScrollToAnchorAction action = new("section1");

        // Act
        bool result = effect.CanHandle(action);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for SetQueryParamsAction.
    /// </summary>
    [Fact]
    public void CanHandleSetQueryParamsActionReturnsTrue()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        SetQueryParamsAction action = new(
            new Dictionary<string, object?>
            {
                ["key"] = "value",
            },
            false);

        // Act
        bool result = effect.CanHandle(action);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that constructor throws when NavigationManager is null.
    /// </summary>
    [Fact]
    public void ConstructorWithNullNavigationManagerThrows()
    {
        // Arrange
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();

        // Act
        Action act = () => _ = new NavigationEffect(null!, mockJs.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationManager");
    }

    /// <summary>
    ///     Verifies that constructor throws when IJSRuntime is null.
    /// </summary>
    [Fact]
    public void ConstructorWithNullJSRuntimeThrows()
    {
        // Arrange
        TestableNavigationManager nav = new();

        // Act
        Action act = () => _ = new NavigationEffect(nav, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("jsRuntime");
    }

    /// <summary>
    ///     Verifies that HandleAsync emits no actions (LocationChangedAction comes from provider).
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncAnyNavigationActionEmitsNoActions()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        NavigateAction action = new("https://example.com/target");
        NavigationState state = CreateDefaultState();

        // Act
        List<IAction> emittedActions = await CollectEmittedActionsAsync(effect, action, state);

        // Assert
        emittedActions.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that HandleAsync for NavigateAction calls NavigateTo with the correct URI.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionCallsNavigateTo()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        NavigateAction action = new("https://example.com/target");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new TestableNavigationManager.NavigationRecord("https://example.com/target", false, false));
    }

    /// <summary>
    ///     Verifies that HandleAsync for NavigateAction with ForceLoad calls NavigateTo with forceLoad true.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithForceLoadCallsNavigateToWithForceLoad()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        NavigateAction action = new("https://example.com/target", true);
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new TestableNavigationManager.NavigationRecord("https://example.com/target", true, false));
    }

    /// <summary>
    ///     Verifies that HandleAsync for ReplaceRouteAction calls NavigateTo with ReplaceHistoryEntry option.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncReplaceRouteActionCallsNavigateToWithReplaceOption()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        ReplaceRouteAction action = new("https://example.com/replaced");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new TestableNavigationManager.NavigationRecord("https://example.com/replaced", false, true));
    }

    /// <summary>
    ///     Verifies that HandleAsync for ScrollToAnchorAction navigates to fragment URL.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncScrollToAnchorActionNavigatesToFragmentUrl()
    {
        // Arrange
        TestableNavigationManager nav = new(uri: "https://example.com/page");
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        ScrollToAnchorAction action = new("section1");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new TestableNavigationManager.NavigationRecord("https://example.com/page#section1", false, false));
    }

    /// <summary>
    ///     Verifies that HandleAsync for ScrollToAnchorAction with existing fragment replaces it.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncScrollToAnchorActionWithExistingFragmentReplacesFragment()
    {
        // Arrange
        TestableNavigationManager nav = new(uri: "https://example.com/page#old-section");
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        ScrollToAnchorAction action = new("new-section");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new TestableNavigationManager.NavigationRecord("https://example.com/page#new-section", false, false));
    }

    /// <summary>
    ///     Verifies that HandleAsync for ScrollToAnchorAction with ReplaceHistory uses NavigationOptions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncScrollToAnchorActionWithReplaceHistoryUsesNavigationOptions()
    {
        // Arrange
        TestableNavigationManager nav = new(uri: "https://example.com/page");
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        ScrollToAnchorAction action = new("section1", true);
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new TestableNavigationManager.NavigationRecord("https://example.com/page#section1", false, true));
    }

    /// <summary>
    ///     Verifies that HandleAsync for SetQueryParamsAction navigates with new query parameters.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncSetQueryParamsActionNavigatesToUriWithQueryParams()
    {
        // Arrange
        TestableNavigationManager nav = new(uri: "https://example.com/page");
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        SetQueryParamsAction action = new(
            new Dictionary<string, object?>
            {
                ["key"] = "value",
            },
            false);
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should().ContainSingle();
        nav.Navigations[0].Uri.Should().Contain("key=value");
        nav.Navigations[0].ReplaceHistoryEntry.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that HandleAsync for SetQueryParamsAction with ReplaceHistory uses NavigationOptions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncSetQueryParamsActionWithReplaceHistoryUsesNavigationOptions()
    {
        // Arrange
        TestableNavigationManager nav = new(uri: "https://example.com/page");
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        SetQueryParamsAction action = new(
            new Dictionary<string, object?>
            {
                ["key"] = "value",
            });
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should().ContainSingle();
        nav.Navigations[0].Uri.Should().Contain("key=value");
        nav.Navigations[0].ReplaceHistoryEntry.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that OpenExternalLinkAction invokes window.open via JS interop.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleOpenExternalLinkActionCallsWindowOpen()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        OpenExternalLinkAction action = new("https://external.com/docs");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        mockJs.Verify(
            js => js.InvokeAsync<object>(
                "open",
                It.IsAny<CancellationToken>(),
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].Equals("https://external.com/docs") &&
                    args[1].Equals("_blank"))),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that OpenExternalLinkAction does not emit any actions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleOpenExternalLinkActionEmitsNoActions()
    {
        // Arrange
        TestableNavigationManager nav = new();
        Mock<IJSRuntime> mockJs = CreateMockJSRuntime();
        NavigationEffect effect = new(nav, mockJs.Object);
        OpenExternalLinkAction action = new("https://external.com");
        NavigationState state = CreateDefaultState();

        // Act
        List<IAction> emitted = await CollectEmittedActionsAsync(effect, action, state);

        // Assert
        emitted.Should().BeEmpty();
    }
}

