using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Client;
using Mississippi.Common.Builders.Gateway;
using Mississippi.Common.Builders.Runtime;


namespace Mississippi.Common.Builders.Core.L0Tests;

/// <summary>
///     L0 tests for <see cref="MississippiBuilderHostExtensions" />.
/// </summary>
public sealed class MississippiBuilderHostExtensionsTests
{
    private sealed class TestClientService
    {
        public override string ToString() => nameof(TestClientService);
    }

    /// <summary>
    ///     UseMississippi merges services from a client builder into the host.
    /// </summary>
    [Fact]
    public void UseMississippiShouldMergeClientBuilderServices()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        ClientBuilder builder = ClientBuilder.Create();
        builder.Services.AddSingleton<TestClientService>();
        hostBuilder.UseMississippi(builder);
        bool containsService =
            hostBuilder.Services.Any(descriptor => descriptor.ServiceType == typeof(TestClientService));
        Assert.True(containsService);
    }

    /// <summary>
    ///     UseMississippi returns the same host builder for runtime builders.
    /// </summary>
    [Fact]
    public void UseMississippiShouldSucceedForRuntimeBuilder()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        RuntimeBuilder builder = RuntimeBuilder.Create();
        HostApplicationBuilder result = hostBuilder.UseMississippi(builder);
        Assert.Same(hostBuilder, result);
    }

    /// <summary>
    ///     UseMississippi succeeds when gateway authorization is configured.
    /// </summary>
    [Fact]
    public void UseMississippiShouldSucceedWhenGatewayAuthorizationConfigured()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        GatewayBuilder builder = GatewayBuilder.Create();
        builder.ConfigureAuthorization();
        HostApplicationBuilder result = hostBuilder.UseMississippi(builder);
        Assert.Same(hostBuilder, result);
    }

    /// <summary>
    ///     UseMississippi throws when attaching a second builder instance.
    /// </summary>
    [Fact]
    public void UseMississippiShouldThrowOnSecondAttachment()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        ClientBuilder builder = ClientBuilder.Create();
        hostBuilder.UseMississippi(builder);
        Assert.Throws<InvalidOperationException>(() => hostBuilder.UseMississippi(builder));
    }

    /// <summary>
    ///     UseMississippi throws when gateway authorization is not configured.
    /// </summary>
    [Fact]
    public void UseMississippiShouldThrowWhenGatewayAuthorizationNotConfigured()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        GatewayBuilder builder = GatewayBuilder.Create();
        BuilderValidationException exception =
            Assert.Throws<BuilderValidationException>(() => hostBuilder.UseMississippi(builder));
        BuilderDiagnostic diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("Gateway.AuthorizationNotConfigured", diagnostic.ErrorCode);
    }

    /// <summary>
    ///     UseMississippi throws when hostBuilder is null.
    /// </summary>
    [Fact]
    public void UseMississippiShouldThrowWhenHostBuilderIsNull()
    {
        HostApplicationBuilder? hostBuilder = null;
        ClientBuilder builder = ClientBuilder.Create();
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => hostBuilder!.UseMississippi(builder));
        Assert.Equal("hostBuilder", exception.ParamName);
    }
}