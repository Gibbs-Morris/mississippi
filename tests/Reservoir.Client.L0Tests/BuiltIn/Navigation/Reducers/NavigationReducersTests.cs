using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;


namespace Mississippi.Reservoir.Client.L0Tests.BuiltIn.Navigation.Reducers;

/// <summary>
///     Unit tests for <see cref="NavigationReducers" />.
/// </summary>
public sealed class NavigationReducersTests
{
    /// <summary>
    ///     Verifies that the initial state has the correct feature key.
    /// </summary>
    [Fact]
    public void InitialStateShouldHaveCorrectFeatureKey()
    {
        // Assert
        Assert.Equal("reservoir:navigation", NavigationState.FeatureKey);
    }

    /// <summary>
    ///     Verifies that the initial state has default values.
    /// </summary>
    [Fact]
    public void InitialStateShouldHaveDefaultValues()
    {
        // Arrange
        NavigationState initialState = new()
        {
            CurrentUri = "about:blank",
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 0,
        };

        // Assert
        Assert.Equal("about:blank", initialState.CurrentUri);
        Assert.Null(initialState.PreviousUri);
        Assert.False(initialState.IsNavigationIntercepted);
        Assert.Equal(0, initialState.NavigationCount);
    }

    /// <summary>
    ///     Verifies that navigation preserves history chain correctly.
    /// </summary>
    [Fact]
    public void OnLocationChangedMultipleTimesShouldTrackPreviousCorrectly()
    {
        // Arrange
        const string page1 = "https://example.com/page1";
        const string page2 = "https://example.com/page2";
        NavigationState state = new()
        {
            CurrentUri = page1,
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 0,
        };
        LocationChangedAction action1 = new(page2, false);

        // Act
        NavigationState stateAfterFirst = NavigationReducers.OnLocationChanged(state, action1);
        LocationChangedAction action2 = new("https://example.com/page3", false);
        NavigationState stateAfterSecond = NavigationReducers.OnLocationChanged(stateAfterFirst, action2);

        // Assert
        Assert.Equal("https://example.com/page3", stateAfterSecond.CurrentUri);
        Assert.Equal(page2, stateAfterSecond.PreviousUri);
        Assert.Equal(2, stateAfterSecond.NavigationCount);
    }

    /// <summary>
    ///     Verifies that OnLocationChanged increments the navigation count.
    /// </summary>
    [Fact]
    public void OnLocationChangedShouldIncrementNavigationCount()
    {
        // Arrange
        NavigationState initialState = new()
        {
            CurrentUri = "https://example.com/",
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 5,
        };
        LocationChangedAction action = new("https://example.com/new", false);

        // Act
        NavigationState result = NavigationReducers.OnLocationChanged(initialState, action);

        // Assert
        Assert.Equal(6, result.NavigationCount);
    }

    /// <summary>
    ///     Verifies that OnLocationChanged moves current URI to previous.
    /// </summary>
    [Fact]
    public void OnLocationChangedShouldSetPreviousUriFromCurrentUri()
    {
        // Arrange
        const string originalUri = "https://example.com/original";
        NavigationState initialState = new()
        {
            CurrentUri = originalUri,
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 0,
        };
        LocationChangedAction action = new("https://example.com/new-page", false);

        // Act
        NavigationState result = NavigationReducers.OnLocationChanged(initialState, action);

        // Assert
        Assert.Equal(originalUri, result.PreviousUri);
    }

    /// <summary>
    ///     Verifies that OnLocationChanged updates CurrentUri from the action.
    /// </summary>
    [Fact]
    public void OnLocationChangedShouldUpdateCurrentUri()
    {
        // Arrange
        NavigationState initialState = new()
        {
            CurrentUri = "https://example.com/initial",
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 0,
        };
        LocationChangedAction action = new("https://example.com/new-page", false);

        // Act
        NavigationState result = NavigationReducers.OnLocationChanged(initialState, action);

        // Assert
        Assert.Equal("https://example.com/new-page", result.CurrentUri);
    }

    /// <summary>
    ///     Verifies that OnLocationChanged updates the IsNavigationIntercepted flag.
    /// </summary>
    [Fact]
    public void OnLocationChangedShouldUpdateIsNavigationIntercepted()
    {
        // Arrange
        NavigationState initialState = new()
        {
            CurrentUri = "https://example.com/",
            PreviousUri = null,
            IsNavigationIntercepted = false,
            NavigationCount = 0,
        };
        LocationChangedAction action = new("https://example.com/intercepted", true);

        // Act
        NavigationState result = NavigationReducers.OnLocationChanged(initialState, action);

        // Assert
        Assert.True(result.IsNavigationIntercepted);
    }
}