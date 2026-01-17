using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Abstractions.State;


namespace Mississippi.Inlet.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionCache" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Core")]
[AllureSubSuite("ProjectionCache")]
public sealed class ProjectionCacheTests
{
    private readonly ProjectionCache sut = new();

    /// <summary>
    ///     Test projection record for testing purposes.
    /// </summary>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     GetProjectionError returns error after SetError.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionErrorReturnsErrorAfterSetError()
    {
        // Arrange
        InvalidOperationException error = new("Test error");
        sut.SetError<TestProjection>("entity-1", error);

        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     GetProjectionError returns null for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionErrorReturnsNullForNonExistentEntity()
    {
        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionError throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void GetProjectionErrorThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjectionError<TestProjection>(null!));

    /// <summary>
    ///     GetProjection returns data after SetLoaded.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionReturnsDataAfterSetLoaded()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        sut.SetLoaded("entity-1", projection, 5L);

        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    /// <summary>
    ///     GetProjection returns null for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionReturnsNullForNonExistentEntity()
    {
        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionState returns null for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionStateReturnsNullWhenNotExists()
    {
        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("non-existent");

        // Assert
        Assert.Null(state);
    }

    /// <summary>
    ///     GetProjectionState returns state after SetLoaded.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionStateReturnsStateWhenExists()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        sut.SetLoaded("entity-1", projection, 5L);

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.NotNull(state.Data);
        Assert.Equal("Test", state.Data.Name);
        Assert.Equal(5L, state.Version);
    }

    /// <summary>
    ///     GetProjectionState throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void GetProjectionStateThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjectionState<TestProjection>(null!));

    /// <summary>
    ///     GetProjection throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void GetProjectionThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjection<TestProjection>(null!));

    /// <summary>
    ///     GetProjectionVersion returns -1 for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionVersionReturnsNegativeOneForNonExistentEntity()
    {
        // Act
        long result = sut.GetProjectionVersion<TestProjection>("non-existent");

        // Assert
        Assert.Equal(-1, result);
    }

    /// <summary>
    ///     GetProjectionVersion returns version after SetLoaded.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionVersionReturnsVersionAfterSetLoaded()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        sut.SetLoaded("entity-1", projection, 10L);

        // Act
        long result = sut.GetProjectionVersion<TestProjection>("entity-1");

        // Assert
        Assert.Equal(10L, result);
    }

    /// <summary>
    ///     GetProjectionVersion throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void GetProjectionVersionThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjectionVersion<TestProjection>(null!));

    /// <summary>
    ///     IsProjectionConnected returns false for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void IsProjectionConnectedReturnsFalseForNonExistentEntity()
    {
        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionConnected returns true after SetConnection with true.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void IsProjectionConnectedReturnsTrueAfterSetConnection()
    {
        // Arrange
        sut.SetConnection<TestProjection>("entity-1", true);

        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsProjectionConnected throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void IsProjectionConnectedThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.IsProjectionConnected<TestProjection>(null!));

    /// <summary>
    ///     IsProjectionLoading returns false for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void IsProjectionLoadingReturnsFalseForNonExistentEntity()
    {
        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionLoading returns true after SetLoading.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void IsProjectionLoadingReturnsTrueAfterSetLoading()
    {
        // Arrange
        sut.SetLoading<TestProjection>("entity-1");

        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsProjectionLoading throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void IsProjectionLoadingThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.IsProjectionLoading<TestProjection>(null!));

    /// <summary>
    ///     SetConnection throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void SetConnectionThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.SetConnection<TestProjection>(null!, true));

    /// <summary>
    ///     SetConnection updates existing state preserving data.
    /// </summary>
    [Fact]
    [AllureFeature("State Updates")]
    public void SetConnectionUpdatesExistingStatePreservingData()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        sut.SetLoaded("entity-1", projection, 5L);

        // Act
        sut.SetConnection<TestProjection>("entity-1", true);

        // Assert
        Assert.True(sut.IsProjectionConnected<TestProjection>("entity-1"));
        Assert.NotNull(sut.GetProjection<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     SetError throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void SetErrorThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.SetError<TestProjection>(
            null!,
            new InvalidOperationException("Test")));

    /// <summary>
    ///     SetError throws when exception is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void SetErrorThrowsArgumentNullExceptionWhenExceptionIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.SetError<TestProjection>("entity-1", null!));

    /// <summary>
    ///     SetLoaded throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void SetLoadedThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.SetLoaded(null!, new TestProjection("Test"), 1L));

    /// <summary>
    ///     SetLoading throws when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void SetLoadingThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.SetLoading<TestProjection>(null!));

    /// <summary>
    ///     SetUpdated delegates to SetLoaded.
    /// </summary>
    [Fact]
    [AllureFeature("State Updates")]
    public void SetUpdatedDelegatesToSetLoaded()
    {
        // Arrange
        TestProjection projection = new("Test", 42);

        // Act
        sut.SetUpdated("entity-1", projection, 10L);

        // Assert
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(10L, sut.GetProjectionVersion<TestProjection>("entity-1"));
    }
}