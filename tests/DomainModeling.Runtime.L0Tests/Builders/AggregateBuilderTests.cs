using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Runtime.Builders;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Builders;

/// <summary>
///     Tests for <see cref="AggregateBuilder" />.
/// </summary>
public sealed class AggregateBuilderTests
{
    [Description("Test stub")]
    private sealed class AnotherFakeAggregate;

    [Description("Test stub")]
    private sealed class FakeAggregate;

    /// <summary>
    ///     Constructor with null services should throw.
    /// </summary>
    [Fact]
    public void ConstructorWithNullServicesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateBuilder(null!));
    }

    /// <summary>
    ///     EnsureNotDuplicate should allow different aggregate types.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateAllowsDifferentTypes()
    {
        // Arrange
        ServiceCollection services = [];
        AggregateBuilder sut = new(services);

        // Act — should not throw for different types.
        sut.EnsureNotDuplicate<FakeAggregate>();
        sut.EnsureNotDuplicate<AnotherFakeAggregate>();

        // Assert
        Assert.Equal(2, sut.RegisteredAggregates.Count);
    }

    /// <summary>
    ///     EnsureNotDuplicate should succeed for first registration.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateSucceedsForFirstRegistration()
    {
        // Arrange
        ServiceCollection services = [];
        AggregateBuilder sut = new(services);

        // Act — should not throw.
        sut.EnsureNotDuplicate<FakeAggregate>();

        // Assert
        Assert.Single(sut.RegisteredAggregates);
    }

    /// <summary>
    ///     EnsureNotDuplicate should throw on second registration of same type.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateThrowsOnDuplicateRegistration()
    {
        // Arrange
        ServiceCollection services = [];
        AggregateBuilder sut = new(services);
        sut.EnsureNotDuplicate<FakeAggregate>();

        // Act & Assert
        MississippiBuilderException ex =
            Assert.Throws<MississippiBuilderException>(() => sut.EnsureNotDuplicate<FakeAggregate>());
        Assert.Equal(MississippiDiagnosticCodes.DuplicateRegistration, ex.DiagnosticCode);
    }

    /// <summary>
    ///     RegisteredAggregates should be empty after construction.
    /// </summary>
    [Fact]
    public void RegisteredAggregatesIsInitiallyEmpty()
    {
        // Arrange
        ServiceCollection services = [];
        AggregateBuilder sut = new(services);

        // Assert
        Assert.Empty(sut.RegisteredAggregates);
    }

    /// <summary>
    ///     Services property should return injected service collection.
    /// </summary>
    [Fact]
    public void ServicesReturnsInjectedServiceCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        AggregateBuilder sut = new(services);

        // Assert
        Assert.Same(services, sut.Services);
    }

    /// <summary>
    ///     Validate should succeed for empty builder.
    /// </summary>
    [Fact]
    public void ValidateSucceedsForEmptyBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        AggregateBuilder sut = new(services);

        // Act — should not throw.
        sut.Validate();
    }

    /// <summary>
    ///     Validate should succeed for builder with registrations.
    /// </summary>
    [Fact]
    public void ValidateSucceedsWithRegistrations()
    {
        // Arrange
        ServiceCollection services = [];
        AggregateBuilder sut = new(services);
        sut.EnsureNotDuplicate<FakeAggregate>();

        // Act — should not throw.
        sut.Validate();
    }
}