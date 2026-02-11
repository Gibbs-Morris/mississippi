using System;
using System.Linq;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Builders;
using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="Mississippi.Aqueduct.AqueductBuilderExtensions" />.
/// </summary>
public sealed class AqueductRegistrationsTests
{
    private sealed class TestHub : Hub;

    private sealed class TestMississippiServerBuilder : IMississippiServerBuilder
    {
        public TestMississippiServerBuilder(
            IServiceCollection services
        ) =>
            Services = services;

        private IServiceCollection Services { get; }

        public IMississippiServerBuilder ConfigureOptions<TOptions>(
            Action<TOptions> configure
        )
            where TOptions : class
        {
            Services.Configure(configure);
            return this;
        }

        public IMississippiServerBuilder ConfigureServices(
            Action<IServiceCollection> configure
        )
        {
            configure(Services);
            return this;
        }
    }

    /// <summary>
    ///     Tests that AddAqueduct throws when builder is null.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct Throws When Builder Is Null")]
    public void AddAqueductShouldThrowWhenBuilderIsNull()
    {
        IMississippiServerBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddAqueduct());
    }

    /// <summary>
    ///     Tests that AddBackplane registers HubLifetimeManager.
    /// </summary>
    [Fact(DisplayName = "AddBackplane Registers HubLifetimeManager")]
    public void AddBackplaneShouldRegisterHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddAqueduct().AddBackplane<TestHub>();

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AqueductHubLifetimeManager<TestHub>), descriptor.ImplementationType);
    }

    /// <summary>
    ///     Tests that AddBackplane returns builder for chaining.
    /// </summary>
    [Fact(DisplayName = "AddBackplane Returns Builder For Chaining")]
    public void AddBackplaneShouldReturnBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        IAqueductServerBuilder result = builder.AddAqueduct().AddBackplane<TestHub>();

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    ///     Tests that AddBackplane throws when builder is null.
    /// </summary>
    [Fact(DisplayName = "AddBackplane Throws When Builder Is Null")]
    public void AddBackplaneShouldThrowWhenBuilderIsNull()
    {
        IMississippiServerBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddAqueduct().AddBackplane<TestHub>());
    }

    /// <summary>
    ///     Tests that TryAdd semantics are used (second registration is ignored).
    /// </summary>
    [Fact(DisplayName = "AddBackplane Uses TryAdd Semantics")]
    public void AddBackplaneShouldUseTryAddSemantics()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act - add twice
        builder.AddAqueduct().AddBackplane<TestHub>();
        builder.AddAqueduct().AddBackplane<TestHub>();

        // Assert - should only have one registration
        int count = services.Count(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.Equal(1, count);
    }

    /// <summary>
    ///     Tests that AddBackplane registers HubLifetimeManager when options configured.
    /// </summary>
    [Fact(DisplayName = "AddBackplane With Options Registers HubLifetimeManager")]
    public void AddBackplaneWithOptionsShouldRegisterHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddAqueduct()
            .ConfigureAqueductOptions(options => options.StreamProviderName = "CustomProvider")
            .AddBackplane<TestHub>();

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    ///     Tests that AddNotifier registers IAqueductNotifier.
    /// </summary>
    [Fact(DisplayName = "AddNotifier Registers IAqueductNotifier")]
    public void AddNotifierShouldRegisterNotifier()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddAqueduct().AddNotifier();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAqueductNotifier));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AqueductNotifier), descriptor.ImplementationType);
    }

    /// <summary>
    ///     Tests that AddNotifier returns builder for chaining.
    /// </summary>
    [Fact(DisplayName = "AddNotifier Returns Builder For Chaining")]
    public void AddNotifierShouldReturnBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        IAqueductServerBuilder result = builder.AddAqueduct().AddNotifier();

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    ///     Tests that TryAdd semantics are used for notifier.
    /// </summary>
    [Fact(DisplayName = "AddNotifier Uses TryAdd Semantics")]
    public void AddNotifierShouldUseTryAddSemantics()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act - add twice
        builder.AddAqueduct().AddNotifier();
        builder.AddAqueduct().AddNotifier();

        // Assert - should only have one registration
        int count = services.Count(d => d.ServiceType == typeof(IAqueductNotifier));
        Assert.Equal(1, count);
    }

    /// <summary>
    ///     Tests that ConfigureOptions configures options when resolved.
    /// </summary>
    [Fact(DisplayName = "ConfigureOptions Configures Options")]
    public void ConfigureOptionsShouldConfigureOptions()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddAqueduct().ConfigureAqueductOptions(options => { options.StreamProviderName = "CustomProvider"; });

        // Build provider and resolve options to trigger configuration
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AqueductOptions>? resolvedOptions = provider.GetService<IOptions<AqueductOptions>>();

        // Assert - configuration action is applied when options are resolved
        Assert.NotNull(resolvedOptions);
        Assert.Equal("CustomProvider", resolvedOptions.Value.StreamProviderName);
    }

    /// <summary>
    ///     Tests that ConfigureOptions returns builder for chaining.
    /// </summary>
    [Fact(DisplayName = "ConfigureOptions Returns Builder For Chaining")]
    public void ConfigureOptionsShouldReturnBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        IAqueductServerBuilder result = builder.AddAqueduct().ConfigureAqueductOptions(_ => { });

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    ///     Tests that ConfigureOptions throws when builder is null.
    /// </summary>
    [Fact(DisplayName = "ConfigureOptions Throws When Builder Is Null")]
    public void ConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        IMississippiServerBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddAqueduct().ConfigureAqueductOptions(_ => { }));
    }

    /// <summary>
    ///     Tests that ConfigureOptions throws when configureOptions is null.
    /// </summary>
    [Fact(DisplayName = "ConfigureOptions Throws When ConfigureOptions Is Null")]
    public void ConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddAqueduct().ConfigureAqueductOptions(null!));
    }
}