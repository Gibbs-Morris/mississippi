using System;
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Inlet.Runtime.Abstractions;


namespace Mississippi.Hosting.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiGatewayBuilder" /> and
///     <see cref="MississippiGatewayWebApplicationBuilderRegistrations" />.
/// </summary>
public sealed class MississippiGatewayBuilderTests
{
    private static void AddRuntimeHostModeMarker(
        IServiceCollection services
    )
    {
        Type runtimeMarkerType = typeof(MississippiRuntimeBuilder).Assembly
            .GetType("Mississippi.Hosting.Runtime.MississippiRuntimeHostModeMarker", throwOnError: true)!;
        object markerInstance = Activator.CreateInstance(runtimeMarkerType) ??
                                throw new InvalidOperationException("Runtime host mode marker instance was null.");
        services.AddSingleton(runtimeMarkerType, markerInstance);
    }

    private static WebApplicationBuilder CreateBuilder() =>
        WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = Environments.Development,
            });

    private sealed class TestHub : Hub;

    /// <summary>
    ///     AddAqueduct should configure Aqueduct options only once.
    /// </summary>
    [Fact]
    public void AddAqueductConfiguresOptionsOnce()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddMississippiGateway(gateway =>
        {
            gateway.AddAqueduct<TestHub>(options => options.StreamProviderName = "StreamProvider");
            gateway.AddAqueduct<TestHub>(options => options.StreamProviderName = "IgnoredSecondCall");
        });
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("StreamProvider", options.StreamProviderName);
    }

    /// <summary>
    ///     AddInletGateway should register aggregate, projection, and inlet infrastructure.
    /// </summary>
    [Fact]
    public void AddInletGatewayRegistersGatewayInfrastructure()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddMississippiGateway(gateway => gateway.AddInletGateway());
        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IAggregateGrainFactory));
        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IUxProjectionGrainFactory));
        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IProjectionBrookRegistry));
    }

    /// <summary>
    ///     AddMississippiGateway configure overload should invoke the callback and return the original web application
    ///     builder.
    /// </summary>
    [Fact]
    public void AddMississippiGatewayConfigureOverloadReturnsOriginalBuilder()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiGatewayBuilder? configuredBuilder = null;
        WebApplicationBuilder result = builder.AddMississippiGateway(gateway => configuredBuilder = gateway);
        Assert.Same(builder, result);
        Assert.NotNull(configuredBuilder);
        Assert.Same(builder.Services, configuredBuilder.Services);
    }

    /// <summary>
    ///     AddMississippiGateway should reject duplicate gateway attachment on the same host.
    /// </summary>
    [Fact]
    public void AddMississippiGatewayRejectsDuplicateAttachment()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddMississippiGateway();
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => builder.AddMississippiGateway());
        Assert.Contains(
            "AddMississippiGateway can only be called once per host",
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiGateway should reject unsupported same-host runtime composition.
    /// </summary>
    [Fact]
    public void AddMississippiGatewayRejectsSameHostRuntimeComposition()
    {
        WebApplicationBuilder builder = CreateBuilder();
        AddRuntimeHostModeMarker(builder.Services);
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => builder.AddMississippiGateway());
        Assert.Contains(
            "runtime and gateway composition cannot share the same host",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains("gateway-only path", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiGateway should return the gateway builder.
    /// </summary>
    [Fact]
    public void AddMississippiGatewayReturnsGatewayBuilder()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiGatewayBuilder gatewayBuilder = builder.AddMississippiGateway();
        Assert.NotNull(gatewayBuilder);
        Assert.Same(builder.Services, gatewayBuilder.Services);
    }

    /// <summary>
    ///     RegisterDomainMappers should execute the callback and return the same gateway builder.
    /// </summary>
    [Fact]
    public void RegisterDomainMappersExecutesCallbackAndReturnsGatewayBuilder()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiGatewayBuilder gatewayBuilder = builder.AddMississippiGateway();
        bool callbackExecuted = false;
        MississippiGatewayBuilder result = gatewayBuilder.RegisterDomainMappers(_ => callbackExecuted = true);
        Assert.True(callbackExecuted);
        Assert.Same(gatewayBuilder, result);
    }
}