using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Client;
using Mississippi.Reservoir.Abstractions;


namespace MississippiTests.Hosting.Client.L0Tests;

/// <summary>
///     Verifies terminal attachment against a host's service collection without a browser runtime.
/// </summary>
public sealed class ClientHostingRegistrationsTests
{
    private static WebAssemblyHostBuilder CreateHost(
        IServiceCollection services
    )
    {
        WebAssemblyHostBuilder builder =
            (WebAssemblyHostBuilder)RuntimeHelpers.GetUninitializedObject(typeof(WebAssemblyHostBuilder));
        FieldInfo? servicesField = typeof(WebAssemblyHostBuilder).GetField(
            "<Services>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(servicesField);
        servicesField.SetValue(builder, services);
        return builder;
    }

    /// <summary>
    ///     The canonical flow commits registrations and keeps host-provided defaults.
    /// </summary>
    [Fact]
    public void AttachCommitsCompositionAndPreservesExistingDescriptors()
    {
        ServiceCollection services = [];
        services.AddSingleton(TimeProvider.System);
        ServiceDescriptor existing = Assert.Single(services);
        WebAssemblyHostBuilder host = CreateHost(services);
        ClientBuilder? captured = null;
        WebAssemblyHostBuilder result = host.UseMississippi(client =>
        {
            captured = client;
            Assert.NotSame(services, client.Services);
            client.Reservoir(_ => { });
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStore));
        });
        Assert.Same(host, result);
        Assert.Same(services, host.Services);
        Assert.Same(existing, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider)));
        Assert.NotNull(captured);
        Assert.True(captured.Services.IsReadOnly);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

    /// <summary>
    ///     A failed callback leaves the host unchanged and permits a corrected attempt.
    /// </summary>
    [Fact]
    public void CallbackFailureDoesNotPartiallyAttach()
    {
        ServiceCollection services = [];
        services.AddSingleton(TimeProvider.System);
        ServiceDescriptor[] original = services.ToArray();
        WebAssemblyHostBuilder host = CreateHost(services);
        InvalidOperationException expected = new("Configuration failed.");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() => host.UseMississippi(client =>
            {
                client.Reservoir(_ => { });
                throw expected;
            })));
        Assert.Equal(original, services);
        host.UseMississippi(client => client.Reservoir(_ => { }));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStore));
    }

    /// <summary>
    ///     Duplicate attachment is rejected before invoking user code or altering the host.
    /// </summary>
    [Fact]
    public void DuplicateAttachReportsStableDiagnosticBeforeCallback()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        host.UseMississippi(_ => { });
        ServiceDescriptor[] original = services.ToArray();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(_ => invoked = true));
        Assert.False(invoked);
        Assert.Equal(original, services);
        BuilderDiagnostic diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("MSB001", diagnostic.Code);
        Assert.Contains("one UseMississippi", diagnostic.Remediation, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Invalid composition never reaches the host.
    /// </summary>
    [Fact]
    public void InvalidBuilderDoesNotCommitStagedRegistrations()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(client =>
            {
                client.Reservoir(_ => { });
                client.Complete();
            }));
        Assert.Equal("MSB002", Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.Same(host, host.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Recursive terminal attachment is also a duplicate and rolls back the reservation.
    /// </summary>
    [Fact]
    public void ReentrantAttachIsRejectedAndCanBeRetried()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(_ => host.UseMississippi(_ => { })));
        Assert.Equal("MSB001", Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.Same(host, host.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Attachment is tracked per host rather than across independent hosts.
    /// </summary>
    [Fact]
    public void SeparateHostsCanEachAttach()
    {
        WebAssemblyHostBuilder first = CreateHost(new ServiceCollection());
        WebAssemblyHostBuilder second = CreateHost(new ServiceCollection());
        Assert.Same(first, first.UseMississippi(_ => { }));
        Assert.Same(second, second.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Invalid arguments do not reserve attachment.
    /// </summary>
    [Fact]
    public void UseMississippiRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ClientHostingRegistrations.UseMississippi(null!, _ => { }));
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        Assert.Throws<ArgumentNullException>(() => host.UseMississippi(null!));
        Assert.Empty(services);
    }
}