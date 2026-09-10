using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Hosting.Client.L0Tests;

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
    ///     A captured Reservoir builder rejects callbacks before application code can run after attachment.
    /// </summary>
    [Fact]
    public void CapturedReservoirRejectsFeatureCallbackAfterAttachment()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        IReservoirBuilder? captured = null;
        host.UseMississippi(client => client.Reservoir(reservoir => captured = reservoir));
        Assert.NotNull(captured);
        ServiceDescriptor[] original = services.ToArray();
        bool invoked = false;
        Assert.Throws<InvalidOperationException>(() =>
            captured.AddFeatureState<ClientLifecycleState>(_ => invoked = true));
        Assert.False(invoked);
        Assert.Equal(original, services);
    }

    /// <summary>
    ///     Even an otherwise idempotent feature registration rejects a completed builder.
    /// </summary>
    [Fact]
    public void CapturedReservoirRejectsRepeatedFeatureAfterAttachment()
    {
        WebAssemblyHostBuilder host = CreateHost(new ServiceCollection());
        IReservoirBuilder? captured = null;
        host.UseMississippi(client => client.Reservoir(reservoir =>
        {
            captured = reservoir;
            reservoir.AddFeatureState<ClientLifecycleState>();
        }));
        Assert.NotNull(captured);
        Assert.Throws<InvalidOperationException>(() => captured.AddFeatureState<ClientLifecycleState>());
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
    ///     Failed client configuration closes all escaped scopes while permitting a fresh retry.
    /// </summary>
    [Fact]
    public void FailedCallbackClosesCapturedBuildersAndServices()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        ClientBuilder? capturedClient = null;
        IReservoirBuilder? capturedReservoir = null;
        IServiceCollection? capturedServices = null;
        Assert.Throws<InvalidOperationException>(() => host.UseMississippi(client =>
        {
            capturedClient = client;
            capturedServices = client.Services;
            client.Reservoir(reservoir => capturedReservoir = reservoir);
            throw new InvalidOperationException("Configuration failed.");
        }));
        Assert.NotNull(capturedClient);
        Assert.NotNull(capturedReservoir);
        Assert.NotNull(capturedServices);
        Assert.True(capturedServices.IsReadOnly);
        Assert.Empty(services);
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            capturedClient.Reservoir(_ => invoked = true));
        Assert.Equal("MSB003", Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Throws<InvalidOperationException>(() =>
            capturedReservoir.AddFeatureState<ClientLifecycleState>(_ => invoked = true));
        Assert.False(invoked);
        Assert.Throws<InvalidOperationException>(() => capturedServices.AddSingleton(TimeProvider.System));
        host.UseMississippi(client => client.Reservoir(reservoir => reservoir.AddFeatureState<ClientLifecycleState>()));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

    /// <summary>
    ///     Feature-scoped builders cannot silently accept registrations after their callback has returned.
    /// </summary>
    [Fact]
    public void FeatureScopeBecomesReadOnlyWhenItsCallbackReturns()
    {
        WebAssemblyHostBuilder host = CreateHost(new ServiceCollection());
        IReservoirFeatureBuilder<ClientLifecycleState>? captured = null;
        host.UseMississippi(client => client.Reservoir(reservoir =>
        {
            reservoir.AddFeatureState<ClientLifecycleState>(feature => captured = feature);
            Assert.NotNull(captured);
            Assert.True(captured.Services.IsReadOnly);
        }));
        Assert.NotNull(captured);
        Assert.Throws<InvalidOperationException>(() => captured.Services.AddSingleton(TimeProvider.System));
    }

    /// <summary>
    ///     Changes to a captured host are reported without being overwritten by the staged graph.
    /// </summary>
    /// <param name="replaceExisting">Whether to replace an existing descriptor instead of adding one.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HostServiceChangesFailWithoutLosingRegistrations(
        bool replaceExisting
    )
    {
        ServiceCollection services = [];
        services.AddSingleton(TimeProvider.System);
        WebAssemblyHostBuilder host = CreateHost(services);
        ClientBuilder? captured = null;
        ServiceDescriptor hostOwned = ServiceDescriptor.Singleton("host-owned");
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(client =>
            {
                captured = client;
                client.Reservoir(_ => { });
                if (replaceExisting)
                {
                    host.Services[0] = hostOwned;
                }
                else
                {
                    host.Services.Add(hostOwned);
                }
            }));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesChanged, Assert.Single(exception.Diagnostics).Code);
        Assert.Contains(hostOwned, services);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStore));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ClientAttachment));
        Assert.NotNull(captured);
        Assert.True(captured.Services.IsReadOnly);
        host.UseMississippi(client => client.Reservoir(_ => { }));
        Assert.Contains(hostOwned, services);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ClientAttachment));
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
        Assert.Equal(BuilderDiagnosticCodes.BuilderAlreadyAttached, Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.Same(host, host.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Freezing captured host services cannot mask callback failures or report a duplicate on the next attempt.
    /// </summary>
    /// <param name="throwFromCallback">Whether the callback also throws after freezing host services.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadOnlyHostDuringConfigurationReportsTheCauseAndClosesTheScope(
        bool throwFromCallback
    )
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        ClientBuilder? captured = null;
        InvalidOperationException expected = new("Application configuration failed.");
        Exception? exception = Record.Exception(() => host.UseMississippi(client =>
        {
            captured = client;
            client.Reservoir(_ => { });
            services.MakeReadOnly();
            if (throwFromCallback)
            {
                throw expected;
            }
        }));
        if (throwFromCallback)
        {
            Assert.Same(expected, exception);
        }
        else
        {
            BuilderValidationException validation = Assert.IsType<BuilderValidationException>(exception);
            Assert.Equal(BuilderDiagnosticCodes.HostServicesReadOnly, Assert.Single(validation.Diagnostics).Code);
        }

        Assert.NotNull(captured);
        Assert.True(captured.Services.IsReadOnly);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStore));
        bool invoked = false;
        BuilderValidationException retry = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(_ => invoked = true));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesReadOnly, Assert.Single(retry.Diagnostics).Code);
        Assert.False(invoked);
    }

    /// <summary>
    ///     An already frozen host fails before reservation or application configuration.
    /// </summary>
    [Fact]
    public void ReadOnlyHostIsRejectedBeforeConfiguration()
    {
        ServiceCollection services = [];
        services.MakeReadOnly();
        WebAssemblyHostBuilder host = CreateHost(services);
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(_ => invoked = true));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesReadOnly, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Empty(services);
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
        Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.Same(host, host.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Host descriptor rewrites cannot leave replacement or duplicate reservations after rejected composition.
    /// </summary>
    /// <param name="replaceReservation">Whether to replace the original reservation instead of duplicating it.</param>
    /// <param name="keyedReservation">Whether the rewritten reservation uses a service key.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RewrittenReservationsDoNotBlockRetry(
        bool replaceReservation,
        bool keyedReservation
    )
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => host.UseMississippi(_ =>
        {
            ServiceDescriptor reservation = Assert.Single(services);
            if (replaceReservation)
            {
                host.Services.Remove(reservation);
            }

            if (keyedReservation)
            {
                host.Services.AddKeyedSingleton(reservation.ServiceType, "copy", reservation.ImplementationInstance!);
            }
            else
            {
                host.Services.AddSingleton(reservation.ServiceType, reservation.ImplementationInstance!);
            }

            host.Services.AddSingleton(TimeProvider.System);
        }));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesChanged, Assert.Single(exception.Diagnostics).Code);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ClientAttachment));
        Assert.Equal(typeof(TimeProvider), Assert.Single(services).ServiceType);
        host.UseMississippi(_ => { });
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ClientAttachment));
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