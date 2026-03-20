using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Runtime.Builders;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Builders;

/// <summary>
///     Tests for <see cref="ProjectionBuilder" />.
/// </summary>
public sealed class ProjectionBuilderTests
{
    [Description("Test stub")]
    private sealed class FakeProjection;

    /// <summary>
    ///     Constructor with null services should throw.
    /// </summary>
    [Fact]
    public void ConstructorWithNullServicesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectionBuilder(null!));
    }

    /// <summary>
    ///     EnsureNotDuplicate should succeed for first registration.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateSucceedsForFirstRegistration()
    {
        ServiceCollection services = [];
        ProjectionBuilder sut = new(services);
        sut.EnsureNotDuplicate<FakeProjection>();
        Assert.Single(sut.RegisteredProjections);
    }

    /// <summary>
    ///     EnsureNotDuplicate should throw on second registration of same type.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateThrowsOnDuplicate()
    {
        ServiceCollection services = [];
        ProjectionBuilder sut = new(services);
        sut.EnsureNotDuplicate<FakeProjection>();
        MississippiBuilderException ex =
            Assert.Throws<MississippiBuilderException>(() => sut.EnsureNotDuplicate<FakeProjection>());
        Assert.Equal(MississippiDiagnosticCodes.DuplicateRegistration, ex.DiagnosticCode);
    }

    /// <summary>
    ///     Services property should return injected service collection.
    /// </summary>
    [Fact]
    public void ServicesReturnsInjectedServiceCollection()
    {
        ServiceCollection services = [];
        ProjectionBuilder sut = new(services);
        Assert.Same(services, sut.Services);
    }

    /// <summary>
    ///     Validate should succeed for empty builder.
    /// </summary>
    [Fact]
    public void ValidateSucceedsForEmptyBuilder()
    {
        ServiceCollection services = [];
        ProjectionBuilder sut = new(services);
        sut.Validate();
    }
}