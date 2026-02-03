using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Effects;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;


namespace Mississippi.Reservoir.Blazor.L0Tests.BuiltIn.Navigation.Effects;

/// <summary>
///     Unit tests for <see cref="NavigationEffect" />.
/// </summary>
public sealed class NavigationEffectTests
{
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
        NavigateAction action = new("https://example.com");

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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
        // Act
        Action act = () => _ = new NavigationEffect(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationManager");
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
    ///     Verifies that NavigateAction rejects external absolute URIs.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithExternalUriThrows()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        NavigateAction action = new("https://contoso.example/target");
        NavigationState state = CreateDefaultState();

        // Act
        Func<Task> act = () => ConsumeEffectAsync(effect, action, state);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*External navigation*");
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
        NavigationEffect effect = new(nav);
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
    ///     Verifies that NavigateAction rejects non-http/https absolute URIs.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithNonHttpSchemeThrows()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        NavigateAction action = new("mailto:test@example.com");
        NavigationState state = CreateDefaultState();

        // Act
        Func<Task> act = () => ConsumeEffectAsync(effect, action, state);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*External navigation*");
    }

    /// <summary>
    ///     Verifies that NavigateAction rejects protocol-relative URIs.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithProtocolRelativeUriThrows()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        NavigateAction action = new("//contoso.example/target");
        NavigationState state = CreateDefaultState();

        // Act
        Func<Task> act = () => ConsumeEffectAsync(effect, action, state);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*External navigation*");
    }

    /// <summary>
    ///     Verifies that NavigateAction accepts relative paths without a leading slash.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithRelativePathCallsNavigateTo()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        NavigateAction action = new("investigations");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new TestableNavigationManager.NavigationRecord("investigations", false, false));
    }

    /// <summary>
    ///     Verifies that NavigateAction accepts root-relative URIs with query and fragment.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithRootRelativeQueryAndFragmentCallsNavigateTo()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        NavigateAction action = new("/investigations?filter=high#top");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new TestableNavigationManager.NavigationRecord("/investigations?filter=high#top", false, false));
    }

    /// <summary>
    ///     Verifies that NavigateAction accepts root-relative URIs.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncNavigateActionWithRootRelativeUriCallsNavigateTo()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        NavigateAction action = new("/investigations");
        NavigationState state = CreateDefaultState();

        // Act
        await ConsumeEffectAsync(effect, action, state);

        // Assert
        nav.Navigations.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new TestableNavigationManager.NavigationRecord("/investigations", false, false));
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
        NavigationEffect effect = new(nav);
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
    ///     Verifies that ReplaceRouteAction rejects external absolute URIs.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncReplaceRouteActionWithExternalUriThrows()
    {
        // Arrange
        TestableNavigationManager nav = new();
        NavigationEffect effect = new(nav);
        ReplaceRouteAction action = new("https://contoso.example/replaced");
        NavigationState state = CreateDefaultState();

        // Act
        Func<Task> act = () => ConsumeEffectAsync(effect, action, state);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*External navigation*");
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
        NavigationEffect effect = new(nav);
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
}