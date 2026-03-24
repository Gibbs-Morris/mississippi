using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiRuntimeBuilder" /> and
///     <see cref="MississippiRuntimeHostBuilderRegistrations" />.
/// </summary>
public sealed class MississippiRuntimeBuilderTests
{
    private sealed class TestSiloBuilder : ISiloBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();

        public IConfiguration Configuration { get; } = new ConfigurationBuilder().Build();
    }

    private sealed class TestMarker
    {
        public string Value { get; } = nameof(TestMarker);
    }

    private static MississippiRuntimeBuilderState GetState(
        IServiceCollection services
    )
    {
        ServiceDescriptor descriptor = Assert.Single(
            services,
            serviceDescriptor => serviceDescriptor.ServiceType == typeof(MississippiRuntimeBuilderState));
        MississippiRuntimeBuilderState? state = descriptor.ImplementationInstance as MississippiRuntimeBuilderState;
        Assert.NotNull(state);
        return state;
    }

    private static void AddCompetingOrleansOwnershipMarker(
        IServiceCollection services
    )
    {
        services.AddSingleton(new MississippiCompetingOrleansOwnershipMarker());
    }

    private static void AddGatewayHostModeMarker(
        IServiceCollection services
    )
    {
        services.AddSingleton(new MississippiGatewayHostModeMarker());
    }

    /// <summary>
    ///     AddMississippiRuntime configure overload should invoke the callback and return the original host builder.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeConfigureOverloadReturnsOriginalBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        MississippiRuntimeBuilder? configuredBuilder = null;
        WebApplicationBuilder result = builder.AddMississippiRuntime(runtime => configuredBuilder = runtime);
        Assert.Same(builder, result);
        Assert.NotNull(configuredBuilder);
        Assert.Same(builder.Services, configuredBuilder.Services);
    }

    /// <summary>
    ///     AddMississippiRuntime should return the runtime builder.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeReturnsRuntimeBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        Assert.NotNull(runtimeBuilder);
        Assert.Same(builder.Services, runtimeBuilder.Services);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject duplicate runtime attachment on the same host.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsDuplicateAttachment()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddMississippiRuntime();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());
        Assert.Contains("AddMississippiRuntime can only be called once per host", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject unsupported same-host gateway composition.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsSameHostGatewayComposition()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        AddGatewayHostModeMarker(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());
        Assert.Contains("runtime and gateway composition cannot share the same host", exception.Message, StringComparison.Ordinal);
        Assert.Contains("runtime-only path", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Orleans should queue additional silo configuration on the runtime-owned state object.
    /// </summary>
    [Fact]
    public void OrleansQueuesConfigurationOnRuntimeState()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.Services.AddSingleton<TestMarker>());
        MississippiRuntimeBuilderState state = GetState(builder.Services);
        Assert.Single(state.QueuedOrleansConfigurations);
    }

    /// <summary>
    ///     Queued Orleans callbacks should replay against the silo builder in registration order.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationReplaysQueuedCallbacksInOrder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.Services.AddSingleton(new TestMarker()));
        runtimeBuilder.Orleans(silo => silo.Services.AddSingleton("queued-second"));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        TestSiloBuilder siloBuilder = new();

        state.ApplyOrleansConfiguration(siloBuilder);

        object[] implementations = siloBuilder.Services
            .Where(descriptor => descriptor.ImplementationInstance is not null)
            .Select(descriptor => descriptor.ImplementationInstance!)
            .ToArray();

        Assert.Collection(
            implementations,
            implementation => Assert.IsType<TestMarker>(implementation),
            implementation => Assert.Equal("queued-second", implementation));
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject same-host gateway composition added after runtime attachment.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsSameHostGatewayCompositionAddedAfterRuntimeAttachment()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddMississippiRuntime();
        AddGatewayHostModeMarker(builder.Services);

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("runtime and gateway composition cannot share the same host", exception.Message, StringComparison.Ordinal);
        Assert.Contains("runtime-only path", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject competing Orleans ownership added after runtime attachment.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsCompetingOrleansOwnershipAddedAfterRuntimeAttachment()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddMississippiRuntime();
        AddCompetingOrleansOwnershipMarker(builder.Services);

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("runtime owns the top-level Orleans silo attachment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MississippiRuntimeBuilder.Orleans(...)", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject competing Orleans ownership introduced through queued callback execution.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsCompetingOrleansOwnershipAddedThroughQueuedCallback()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => AddCompetingOrleansOwnershipMarker(silo.Services));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("runtime owns the top-level Orleans silo attachment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MississippiRuntimeBuilder.Orleans(...)", exception.Message, StringComparison.Ordinal);
    }
}