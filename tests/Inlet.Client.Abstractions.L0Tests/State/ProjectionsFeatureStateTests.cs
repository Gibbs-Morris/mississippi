using System;
using System.Collections.Immutable;

using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.Abstractions.L0Tests.State;

/// <summary>
///     Tests for <see cref="ProjectionsFeatureState" />.
/// </summary>
public sealed class ProjectionsFeatureStateTests
{
    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     Another projection type for testing type separation.
    /// </summary>
    private sealed record OtherProjection(string Id);

    /// <summary>
    ///     FeatureKey should return "projections".
    /// </summary>
    [Fact]
    public void FeatureKeyReturnsProjections() =>
        Assert.Equal("projections", ProjectionsFeatureState.FeatureKey);

    /// <summary>
    ///     Default state should have empty entries.
    /// </summary>
    [Fact]
    public void DefaultStateHasEmptyEntries()
    {
        // Arrange & Act
        ProjectionsFeatureState sut = new();

        // Assert
        Assert.Empty(sut.Entries);
    }

    /// <summary>
    ///     GetKey should return composite key with full type name and entity id.
    /// </summary>
    [Fact]
    public void GetKeyReturnsCompositeKey()
    {
        // Act
        string key = ProjectionsFeatureState.GetKey<TestProjection>("entity-1");

        // Assert
        Assert.Contains("TestProjection", key);
        Assert.Contains("entity-1", key);
        Assert.Contains(":", key);
    }

    /// <summary>
    ///     GetEntry should return null for non-existent entity.
    /// </summary>
    [Fact]
    public void GetEntryReturnsNullForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        ProjectionEntry<TestProjection>? result = sut.GetEntry<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetEntry should return entry after WithEntry.
    /// </summary>
    [Fact]
    public void GetEntryReturnsEntryAfterWithEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> entry = new(new TestProjection("Test", 42), 10L, false, true, null);

        // Act
        ProjectionsFeatureState updated = sut.WithEntry("entity-1", entry);
        ProjectionEntry<TestProjection>? result = updated.GetEntry<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("Test", result.Data.Name);
        Assert.Equal(42, result.Data.Value);
        Assert.Equal(10L, result.Version);
        Assert.True(result.IsConnected);
        Assert.False(result.IsLoading);
    }

    /// <summary>
    ///     GetProjection should return null for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionReturnsNullForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjection should return data after WithEntry.
    /// </summary>
    [Fact]
    public void GetProjectionReturnsDataAfterWithEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        TestProjection projection = new("Test", 42);
        ProjectionEntry<TestProjection> entry = new(projection, 10L, false, false, null);

        // Act
        ProjectionsFeatureState updated = sut.WithEntry("entity-1", entry);
        TestProjection? result = updated.GetProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    /// <summary>
    ///     GetProjectionError should return null for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionErrorReturnsNullForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionError should return error after WithEntry with error.
    /// </summary>
    [Fact]
    public void GetProjectionErrorReturnsErrorAfterWithEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        InvalidOperationException error = new("Test error");
        ProjectionEntry<TestProjection> entry = new(null, -1, false, false, error);

        // Act
        ProjectionsFeatureState updated = sut.WithEntry("entity-1", entry);
        Exception? result = updated.GetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     GetProjectionVersion should return -1 for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionVersionReturnsNegativeOneForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        long result = sut.GetProjectionVersion<TestProjection>("non-existent");

        // Assert
        Assert.Equal(-1, result);
    }

    /// <summary>
    ///     GetProjectionVersion should return version after WithEntry.
    /// </summary>
    [Fact]
    public void GetProjectionVersionReturnsVersionAfterWithEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> entry = new(null, 25L, false, false, null);

        // Act
        ProjectionsFeatureState updated = sut.WithEntry("entity-1", entry);
        long result = updated.GetProjectionVersion<TestProjection>("entity-1");

        // Assert
        Assert.Equal(25L, result);
    }

    /// <summary>
    ///     IsProjectionConnected should return false for non-existent entity.
    /// </summary>
    [Fact]
    public void IsProjectionConnectedReturnsFalseForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionConnected should return true after WithEntry with IsConnected=true.
    /// </summary>
    [Fact]
    public void IsProjectionConnectedReturnsTrueAfterWithEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> entry = new(null, -1, false, true, null);

        // Act
        ProjectionsFeatureState updated = sut.WithEntry("entity-1", entry);
        bool result = updated.IsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsProjectionLoading should return false for non-existent entity.
    /// </summary>
    [Fact]
    public void IsProjectionLoadingReturnsFalseForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionLoading should return true after WithEntry with IsLoading=true.
    /// </summary>
    [Fact]
    public void IsProjectionLoadingReturnsTrueAfterWithEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> entry = new(null, -1, true, false, null);

        // Act
        ProjectionsFeatureState updated = sut.WithEntry("entity-1", entry);
        bool result = updated.IsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     WithEntry should not mutate original state.
    /// </summary>
    [Fact]
    public void WithEntryDoesNotMutateOriginalState()
    {
        // Arrange
        ProjectionsFeatureState original = new();
        ProjectionEntry<TestProjection> entry = new(new TestProjection("Test"), 5L, false, false, null);

        // Act
        ProjectionsFeatureState updated = original.WithEntry("entity-1", entry);

        // Assert
        Assert.Null(original.GetProjection<TestProjection>("entity-1"));
        Assert.NotNull(updated.GetProjection<TestProjection>("entity-1"));
        Assert.NotSame(original, updated);
    }

    /// <summary>
    ///     WithEntryTransform should transform existing entry.
    /// </summary>
    [Fact]
    public void WithEntryTransformTransformsExistingEntry()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> entry = new(new TestProjection("Original", 10), 5L, false, false, null);
        sut = sut.WithEntry("entity-1", entry);

        // Act
        ProjectionsFeatureState updated = sut.WithEntryTransform<TestProjection>(
            "entity-1",
            e => e with { IsConnected = true, Version = 10L });

        // Assert
        ProjectionEntry<TestProjection>? result = updated.GetEntry<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.True(result.IsConnected);
        Assert.Equal(10L, result.Version);
        Assert.Equal("Original", result.Data!.Name);
    }

    /// <summary>
    ///     WithEntryTransform should use Empty entry for non-existent entity.
    /// </summary>
    [Fact]
    public void WithEntryTransformUsesEmptyEntryForNonExistentEntity()
    {
        // Arrange
        ProjectionsFeatureState sut = new();

        // Act
        ProjectionsFeatureState updated = sut.WithEntryTransform<TestProjection>(
            "entity-1",
            e => e with { IsLoading = true });

        // Assert
        Assert.True(updated.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.Equal(-1, updated.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Different projection types should be stored separately.
    /// </summary>
    [Fact]
    public void DifferentProjectionTypesAreStoredSeparately()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> testEntry = new(new TestProjection("Test", 1), 10L, false, false, null);
        ProjectionEntry<OtherProjection> otherEntry = new(new OtherProjection("Other"), 20L, true, true, null);

        // Act
        sut = sut.WithEntry("entity-1", testEntry);
        sut = sut.WithEntry("entity-1", otherEntry);

        // Assert - same entity ID but different types
        Assert.Equal("Test", sut.GetProjection<TestProjection>("entity-1")!.Name);
        Assert.Equal("Other", sut.GetProjection<OtherProjection>("entity-1")!.Id);
        Assert.Equal(10L, sut.GetProjectionVersion<TestProjection>("entity-1"));
        Assert.Equal(20L, sut.GetProjectionVersion<OtherProjection>("entity-1"));
        Assert.False(sut.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.True(sut.IsProjectionLoading<OtherProjection>("entity-1"));
    }

    /// <summary>
    ///     Multiple entities of same type should be stored separately.
    /// </summary>
    [Fact]
    public void MultipleEntitiesOfSameTypeAreStoredSeparately()
    {
        // Arrange
        ProjectionsFeatureState sut = new();
        ProjectionEntry<TestProjection> entry1 = new(new TestProjection("First", 1), 10L, false, false, null);
        ProjectionEntry<TestProjection> entry2 = new(new TestProjection("Second", 2), 20L, true, true, null);

        // Act
        sut = sut.WithEntry("entity-1", entry1);
        sut = sut.WithEntry("entity-2", entry2);

        // Assert
        Assert.Equal("First", sut.GetProjection<TestProjection>("entity-1")!.Name);
        Assert.Equal("Second", sut.GetProjection<TestProjection>("entity-2")!.Name);
        Assert.Equal(10L, sut.GetProjectionVersion<TestProjection>("entity-1"));
        Assert.Equal(20L, sut.GetProjectionVersion<TestProjection>("entity-2"));
    }
}

/// <summary>
///     Tests for <see cref="ProjectionEntry{T}" />.
/// </summary>
public sealed class ProjectionEntryTests
{
    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    private sealed record TestProjection(string Name);

    /// <summary>
    ///     Empty should have default values.
    /// </summary>
    [Fact]
    public void EmptyHasDefaultValues()
    {
        // Act
        ProjectionEntry<TestProjection> sut = ProjectionEntry<TestProjection>.Empty;

        // Assert
        Assert.Null(sut.Data);
        Assert.Equal(-1, sut.Version);
        Assert.False(sut.IsLoading);
        Assert.False(sut.IsConnected);
        Assert.Null(sut.Error);
    }

    /// <summary>
    ///     With expression should create new entry with modified properties.
    /// </summary>
    [Fact]
    public void WithExpressionCreatesNewEntryWithModifiedProperties()
    {
        // Arrange
        ProjectionEntry<TestProjection> original = new(new TestProjection("Test"), 10L, false, false, null);

        // Act
        ProjectionEntry<TestProjection> updated = original with { IsLoading = true, Version = 20L };

        // Assert
        Assert.True(updated.IsLoading);
        Assert.Equal(20L, updated.Version);
        Assert.Equal("Test", updated.Data!.Name);
        Assert.NotSame(original, updated);
    }
}
