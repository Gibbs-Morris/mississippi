using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Runtime.Abstractions;


namespace Mississippi.Inlet.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="InletSiloRegistrations" />.
/// </summary>
public sealed class InletSiloRegistrationsTests
{
    /// <summary>
    ///     Verifies AddInletSilo can be called multiple times without changing the service collection.
    /// </summary>
    [Fact]
    public void AddInletSiloCanBeCalledMultipleTimes()
    {
        ServiceCollection services = [];
        services.AddInletSilo();
        IServiceCollection result = services.AddInletSilo();
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IProjectionBrookRegistry>());
        Assert.NotNull(provider.GetRequiredService<IProjectionAuthorizationRegistry>());
    }

    /// <summary>
    ///     Verifies AddInletSilo registers projection registries as singletons.
    /// </summary>
    [Fact]
    public void AddInletSiloRegistersAsSingleton()
    {
        ServiceCollection services = [];
        services.AddInletSilo();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry1 = provider.GetRequiredService<IProjectionBrookRegistry>();
        IProjectionBrookRegistry registry2 = provider.GetRequiredService<IProjectionBrookRegistry>();
        IProjectionAuthorizationRegistry authorizationRegistry1 =
            provider.GetRequiredService<IProjectionAuthorizationRegistry>();
        IProjectionAuthorizationRegistry authorizationRegistry2 =
            provider.GetRequiredService<IProjectionAuthorizationRegistry>();
        Assert.Same(registry1, registry2);
        Assert.Same(authorizationRegistry1, authorizationRegistry2);
    }

    /// <summary>
    ///     Verifies AddInletSilo registers both projection registries.
    /// </summary>
    [Fact]
    public void AddInletSiloRegistersProjectionRegistries()
    {
        ServiceCollection services = [];
        services.AddInletSilo();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();
        IProjectionAuthorizationRegistry authorizationRegistry =
            provider.GetRequiredService<IProjectionAuthorizationRegistry>();
        Assert.NotNull(registry);
        Assert.NotNull(authorizationRegistry);
    }

    /// <summary>
    ///     Verifies AddInletSilo returns the original service collection.
    /// </summary>
    [Fact]
    public void AddInletSiloReturnsSameCollection()
    {
        ServiceCollection services = [];
        IServiceCollection result = services.AddInletSilo();
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Verifies AddInletSilo rejects null service collections.
    /// </summary>
    [Fact]
    public void AddInletSiloThrowsWhenServicesNull()
    {
        IServiceCollection services = null!;
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddInletSilo());
        Assert.Equal("services", exception.ParamName);
    }
}