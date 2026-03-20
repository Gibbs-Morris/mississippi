using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Runtime.Builders;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Builders;

/// <summary>
///     Tests for <see cref="SagaBuilder" />.
/// </summary>
public sealed class SagaBuilderTests
{
    [Description("Test stub")]
    private sealed class FakeSaga;

    /// <summary>
    ///     Constructor with null services should throw.
    /// </summary>
    [Fact]
    public void ConstructorWithNullServicesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new SagaBuilder(null!));
    }

    /// <summary>
    ///     EnsureNotDuplicate should succeed for first registration.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateSucceedsForFirstRegistration()
    {
        ServiceCollection services = [];
        SagaBuilder sut = new(services);
        sut.EnsureNotDuplicate<FakeSaga>();
        Assert.Single(sut.RegisteredSagas);
    }

    /// <summary>
    ///     EnsureNotDuplicate should throw on second registration of same type.
    /// </summary>
    [Fact]
    public void EnsureNotDuplicateThrowsOnDuplicate()
    {
        ServiceCollection services = [];
        SagaBuilder sut = new(services);
        sut.EnsureNotDuplicate<FakeSaga>();
        MississippiBuilderException ex =
            Assert.Throws<MississippiBuilderException>(() => sut.EnsureNotDuplicate<FakeSaga>());
        Assert.Equal(MississippiDiagnosticCodes.DuplicateRegistration, ex.DiagnosticCode);
    }

    /// <summary>
    ///     Services property should return injected service collection.
    /// </summary>
    [Fact]
    public void ServicesReturnsInjectedServiceCollection()
    {
        ServiceCollection services = [];
        SagaBuilder sut = new(services);
        Assert.Same(services, sut.Services);
    }

    /// <summary>
    ///     Validate should succeed for empty builder.
    /// </summary>
    [Fact]
    public void ValidateSucceedsForEmptyBuilder()
    {
        ServiceCollection services = [];
        SagaBuilder sut = new(services);
        sut.Validate();
    }
}