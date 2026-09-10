using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime.L0Tests;

/// <summary>
///     Verifies terminal runtime attachment and native configuration lifecycle.
/// </summary>
public sealed class RuntimeHostingRegistrationsTests
{
    /// <summary>
    ///     Clearing the original host cannot permit recursive attachment through application or native callbacks.
    /// </summary>
    /// <param name="nativeCallback">Whether recursion occurs in queued native configuration.</param>
    /// <param name="wrapHost">Whether another silo builder wraps the original host services.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ClearedOriginalHostCannotAttachRecursively(
        bool nativeCallback,
        bool wrapHost
    )
    {
        TestSiloBuilder silo = new();
        ISiloBuilder target = wrapHost
            ? Mock.Of<ISiloBuilder>(candidate =>
                (candidate.Services == silo.Services) && (candidate.Configuration == silo.Configuration))
            : silo;
        bool invoked = false;
        Action recurse = () =>
        {
            silo.Services.Clear();
            target.UseMississippi(_ => invoked = true);
        };
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(runtime =>
            {
                if (nativeCallback)
                {
                    runtime.ConfigureSilo(_ => recurse());
                }
                else
                {
                    recurse();
                }
            }));
        Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Empty(silo.Services);
        silo.UseMississippi(_ => { });
        Assert.Single(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     The staged native adapter remains nonterminal even when advanced composition clears its descriptors.
    /// </summary>
    /// <param name="wrapSilo">Whether to wrap the staged silo in another implementation.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearedStagedServicesDoNotPermitRecursiveAttachment(
        bool wrapSilo
    )
    {
        TestSiloBuilder silo = new();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(runtime => runtime.ConfigureSilo(staged =>
            {
                staged.Services.Clear();
                ISiloBuilder target = wrapSilo
                    ? Mock.Of<ISiloBuilder>(candidate =>
                        (candidate.Services == staged.Services) && (candidate.Configuration == staged.Configuration))
                    : staged;
                target.UseMississippi(_ => invoked = true);
            })));
        Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Empty(silo.Services);
        silo.UseMississippi(_ => { });
        Assert.Single(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     A successfully attached host remains terminal after its service descriptors are cleared.
    /// </summary>
    [Fact]
    public void ClearedSuccessfullyAttachedHostCannotAttachAgain()
    {
        TestSiloBuilder silo = new();
        silo.UseMississippi(_ => { });
        silo.Services.Clear();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(_ => invoked = true));
        Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Empty(silo.Services);
    }

    /// <summary>
    ///     Completed runtime builders reject further native configuration without executing it.
    /// </summary>
    [Fact]
    public void CompletedRuntimeRejectsFurtherConfiguration()
    {
        TestSiloBuilder silo = new();
        RuntimeBuilder? captured = null;
        silo.UseMississippi(runtime => captured = runtime);
        Assert.NotNull(captured);
        bool invoked = false;
        Assert.Throws<BuilderValidationException>(() => captured.ConfigureSilo(_ => invoked = true));
        Assert.Throws<BuilderValidationException>(() => captured.ApplyToSilo(silo));
        Assert.False(invoked);
    }

    /// <summary>
    ///     Duplicate attachment fails before invoking application code and preserves the existing graph.
    /// </summary>
    [Fact]
    public void DuplicateAttachmentDoesNotInvokeTheCallback()
    {
        TestSiloBuilder silo = new();
        silo.UseMississippi(_ => { });
        ServiceDescriptor[] original = silo.Services.ToArray();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(_ => invoked = true));
        Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Equal(original, silo.Services);
    }

    /// <summary>
    ///     Empty roots attach without a placeholder domain workload or explicit silo hook.
    /// </summary>
    [Fact]
    public void EmptyRootAttachesAndPreservesHostRegistrations()
    {
        TestSiloBuilder silo = new();
        silo.Services.AddSingleton(TimeProvider.System);
        ServiceDescriptor original = Assert.Single(silo.Services);
        RuntimeBuilder? captured = null;
        Assert.Same(
            silo,
            silo.UseMississippi(runtime =>
            {
                captured = runtime;
                Assert.IsType<IMississippiBuilder>(runtime, false);
                Assert.IsType<IRuntimeBuilder>(runtime, false);
                Assert.Empty(runtime.Validate());
            }));
        Assert.NotNull(captured);
        Assert.Same(original, silo.Services[0]);
        Assert.True(captured.Services.IsReadOnly);
        Assert.Equal(BuilderDiagnosticCodes.BuilderAlreadyAttached, Assert.Single(captured.Validate()).Code);
    }

    /// <summary>
    ///     An explicit hook applies once without publishing staged registrations before terminal completion.
    /// </summary>
    [Fact]
    public void ExplicitSiloApplicationIsNotRepeatedByAttachment()
    {
        TestSiloBuilder silo = new();
        int calls = 0;
        silo.UseMississippi(runtime =>
        {
            runtime.ConfigureSilo(staged =>
            {
                calls++;
                staged.Services.AddSingleton(TimeProvider.System);
            });
            Assert.Same(runtime, runtime.ApplyToSilo(silo));
            Assert.Equal(1, calls);
            Assert.DoesNotContain(silo.Services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
            Assert.Empty(runtime.Validate());
        });
        Assert.Equal(1, calls);
        Assert.Contains(silo.Services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    /// <summary>
    ///     Application callback failures discard services, close escaped scopes, and do not run queued native work.
    /// </summary>
    [Fact]
    public void FailedCallbackClosesTheScopeAndDoesNotApplyNativeWork()
    {
        TestSiloBuilder silo = new();
        RuntimeBuilder? captured = null;
        bool nativeInvoked = false;
        InvalidOperationException expected = new("Application configuration failed.");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() => silo.UseMississippi(runtime =>
            {
                captured = runtime;
                runtime.Services.AddSingleton(TimeProvider.System);
                runtime.ConfigureSilo(_ => nativeInvoked = true);
                throw expected;
            })));
        Assert.NotNull(captured);
        Assert.False(nativeInvoked);
        Assert.Empty(silo.Services);
        Assert.True(captured.Services.IsReadOnly);
        Assert.Equal(BuilderDiagnosticCodes.ConfigurationScopeClosed, Assert.Single(captured.Validate()).Code);
        Assert.Throws<BuilderValidationException>(() => captured.ConfigureSilo(_ => nativeInvoked = true));
        Assert.Throws<BuilderValidationException>(() => captured.ApplyToSilo(silo));
        Assert.False(nativeInvoked);
        silo.UseMississippi(runtime => runtime.ConfigureSilo(staged => staged.Services.AddSingleton("retry")));
        using ServiceProvider provider = silo.Services.BuildServiceProvider();
        Assert.Equal("retry", provider.GetRequiredService<string>());
    }

    /// <summary>
    ///     Native callback failures do not partially publish services and close the staged native adapter.
    /// </summary>
    [Fact]
    public void FailedNativeCallbackDoesNotPartiallyAttach()
    {
        TestSiloBuilder silo = new();
        silo.Services.AddSingleton(TimeProvider.System);
        ServiceDescriptor[] original = silo.Services.ToArray();
        ISiloBuilder? capturedSilo = null;
        InvalidOperationException expected = new("Native configuration failed.");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() => silo.UseMississippi(runtime =>
                runtime.ConfigureSilo(staged =>
                {
                    capturedSilo = staged;
                    staged.Services.AddSingleton("discarded");
                    throw expected;
                }))));
        Assert.NotNull(capturedSilo);
        Assert.True(capturedSilo.Services.IsReadOnly);
        Assert.Equal(original, silo.Services);
        silo.UseMississippi(_ => { });
    }

    /// <summary>
    ///     Direct host mutations fail attachment without overwriting registrations made outside the staged graph.
    /// </summary>
    /// <param name="nativeCallback">Whether the direct mutation occurs inside queued native configuration.</param>
    /// <param name="replaceExisting">Whether to replace an existing descriptor instead of adding one.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HostServiceChangesFailWithoutLosingRegistrations(
        bool nativeCallback,
        bool replaceExisting
    )
    {
        TestSiloBuilder silo = new();
        silo.Services.AddSingleton(TimeProvider.System);
        RuntimeBuilder? captured = null;
        bool nativeInvoked = false;
        ServiceDescriptor hostOwned = ServiceDescriptor.Singleton("host-owned");
        Action changeHostServices = () =>
        {
            if (replaceExisting)
            {
                silo.Services[0] = hostOwned;
            }
            else
            {
                silo.Services.Add(hostOwned);
            }
        };
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(runtime =>
            {
                captured = runtime;
                runtime.Services.AddSingleton(new object());
                runtime.ConfigureSilo(_ =>
                {
                    nativeInvoked = true;
                    if (nativeCallback)
                    {
                        changeHostServices();
                    }
                });
                if (!nativeCallback)
                {
                    changeHostServices();
                }
            }));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesChanged, Assert.Single(exception.Diagnostics).Code);
        Assert.Equal(nativeCallback, nativeInvoked);
        Assert.Contains(hostOwned, silo.Services);
        Assert.DoesNotContain(silo.Services, descriptor => descriptor.ServiceType == typeof(object));
        Assert.DoesNotContain(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
        Assert.NotNull(captured);
        Assert.True(captured.Services.IsReadOnly);
        silo.UseMississippi(_ => { });
        Assert.Contains(hostOwned, silo.Services);
        Assert.Single(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     Native application is bound to one host and one application phase.
    /// </summary>
    [Fact]
    public void NativeApplicationRejectsWrongHostAndRepeatedApplication()
    {
        TestSiloBuilder silo = new();
        TestSiloBuilder other = new();
        silo.UseMississippi(runtime =>
        {
            BuilderValidationException wrongHost = Assert.Throws<BuilderValidationException>(() =>
                runtime.ApplyToSilo(other));
            Assert.Equal(RuntimeBuilderDiagnosticCodes.SiloHostMismatch, Assert.Single(wrongHost.Diagnostics).Code);
            Assert.Empty(other.Services);
            runtime.ApplyToSilo(silo);
            BuilderValidationException duplicate = Assert.Throws<BuilderValidationException>(() =>
                runtime.ApplyToSilo(silo));
            Assert.Equal(
                RuntimeBuilderDiagnosticCodes.DuplicateSiloApplication,
                Assert.Single(duplicate.Diagnostics).Code);
            BuilderValidationException lateConfiguration = Assert.Throws<BuilderValidationException>(() =>
                runtime.ConfigureSilo(_ => { }));
            Assert.Equal(
                RuntimeBuilderDiagnosticCodes.SiloConfigurationAlreadyApplied,
                Assert.Single(lateConfiguration.Diagnostics).Code);
        });
        other.UseMississippi(_ => { });
    }

    /// <summary>
    ///     Native callbacks cannot attach another runtime through the staged silo or a wrapper over its services.
    /// </summary>
    /// <param name="wrapSilo">Whether to wrap the staged silo in another interface implementation.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeCallbackRejectsNestedAttachment(
        bool wrapSilo
    )
    {
        TestSiloBuilder silo = new();
        bool invoked = false;
        silo.UseMississippi(runtime => runtime.ConfigureSilo(staged =>
        {
            ISiloBuilder target = wrapSilo
                ? Mock.Of<ISiloBuilder>(candidate =>
                    (candidate.Services == staged.Services) && (candidate.Configuration == staged.Configuration))
                : staged;
            BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
                target.UseMississippi(_ => invoked = true));
            Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        }));
        Assert.False(invoked);
        Assert.Single(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     Invalid arguments fail without reserving the host or adding work.
    /// </summary>
    [Fact]
    public void NullArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => RuntimeHostingRegistrations.UseMississippi(null!, _ => { }));
        TestSiloBuilder silo = new();
        Assert.Throws<ArgumentNullException>(() => silo.UseMississippi(null!));
        Assert.Empty(silo.Services);
        silo.UseMississippi(runtime =>
        {
            Assert.Throws<ArgumentNullException>(() => runtime.ConfigureSilo(null!));
            Assert.Throws<ArgumentNullException>(() => runtime.ApplyToSilo(null!));
        });
    }

    /// <summary>
    ///     Publication faults restore the original host graph and permit a fresh runtime attempt.
    /// </summary>
    /// <param name="failDuringClear">Whether to fail after clearing instead of while inserting descriptors.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicationFailureRestoresHostAndAllowsRetry(
        bool failDuringClear
    )
    {
        FaultingServiceCollection services = new()
        {
            ShouldFailOnClear = failDuringClear,
        };
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton("original");
        ServiceDescriptor[] original = services.ToArray();
        IConfiguration configuration = Mock.Of<IConfiguration>();
        ISiloBuilder silo = Mock.Of<ISiloBuilder>(candidate =>
            (candidate.Services == services) && (candidate.Configuration == configuration));
        InvalidOperationException expected = new("Host service publication failed.");
        services.Failures.Enqueue(expected);
        RuntimeBuilder? captured = null;
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() => silo.UseMississippi(runtime =>
            {
                captured = runtime;
                runtime.Services.Clear();
                runtime.Services.AddSingleton(new object());
            })));
        Assert.Equal(original, services);
        Assert.NotNull(captured);
        Assert.Equal(BuilderDiagnosticCodes.ConfigurationScopeClosed, Assert.Single(captured.Validate()).Code);
        silo.UseMississippi(_ => { });
        Assert.Same(original[0], services[0]);
        Assert.Same(original[1], services[1]);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     A failed restoration reports both faults instead of concealing either cause.
    /// </summary>
    [Fact]
    public void PublicationRestorationFailureReportsBothCauses()
    {
        FaultingServiceCollection services = new();
        services.AddSingleton("original");
        IConfiguration configuration = Mock.Of<IConfiguration>();
        ISiloBuilder silo = Mock.Of<ISiloBuilder>(candidate =>
            (candidate.Services == services) && (candidate.Configuration == configuration));
        InvalidOperationException publication = new("Publication failed.");
        InvalidOperationException restoration = new("Restoration failed.");
        services.Failures.Enqueue(publication);
        services.Failures.Enqueue(restoration);
        AggregateException exception = Assert.Throws<AggregateException>(() =>
            silo.UseMississippi(runtime => runtime.Services.AddSingleton(new object())));
        Assert.Collection(
            exception.InnerExceptions,
            first => Assert.Same(publication, first),
            second => Assert.Same(restoration, second));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     Freezing host services cannot mask application or native failures, or misdiagnose the next attempt.
    /// </summary>
    /// <param name="nativeCallback">Whether the host is frozen inside queued native configuration.</param>
    /// <param name="throwFromCallback">Whether the callback also throws after freezing host services.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReadOnlyHostDuringConfigurationReportsTheCauseAndClosesTheScope(
        bool nativeCallback,
        bool throwFromCallback
    )
    {
        TestSiloBuilder silo = new();
        ServiceCollection services = Assert.IsType<ServiceCollection>(silo.Services);
        RuntimeBuilder? captured = null;
        InvalidOperationException expected = new("Application configuration failed.");
        Action freezeHost = () =>
        {
            services.MakeReadOnly();
            if (throwFromCallback)
            {
                throw expected;
            }
        };
        Exception? exception = Record.Exception(() => silo.UseMississippi(runtime =>
        {
            captured = runtime;
            runtime.Services.AddSingleton(new object());
            if (nativeCallback)
            {
                runtime.ConfigureSilo(_ => freezeHost());
            }
            else
            {
                freezeHost();
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
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(object));
        bool invoked = false;
        BuilderValidationException retry = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(_ => invoked = true));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesReadOnly, Assert.Single(retry.Diagnostics).Code);
        Assert.False(invoked);
    }

    /// <summary>
    ///     An already frozen host fails before reservation or application configuration.
    /// </summary>
    [Fact]
    public void ReadOnlyHostIsRejectedBeforeConfiguration()
    {
        TestSiloBuilder silo = new();
        Assert.IsType<ServiceCollection>(silo.Services).MakeReadOnly();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(_ => invoked = true));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesReadOnly, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Empty(silo.Services);
    }

    /// <summary>
    ///     Recursive attachment releases its reservation on failure and allows a fresh retry.
    /// </summary>
    [Fact]
    public void RecursiveAttachmentCanBeRetriedAfterFailure()
    {
        TestSiloBuilder silo = new();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(_ => silo.UseMississippi(_ => { })));
        Assert.Equal(BuilderDiagnosticCodes.DuplicateHostAttachment, Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(silo.Services);
        silo.UseMississippi(_ => { });
    }

    /// <summary>
    ///     Rejected host descriptor rewrites remove all role reservations and preserve application registrations.
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
        TestSiloBuilder silo = new();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => silo.UseMississippi(_ =>
        {
            ServiceDescriptor reservation = Assert.Single(silo.Services);
            if (replaceReservation)
            {
                silo.Services.Remove(reservation);
            }

            if (keyedReservation)
            {
                silo.Services.AddKeyedSingleton(reservation.ServiceType, "copy", reservation.ImplementationInstance!);
            }
            else
            {
                silo.Services.AddSingleton(reservation.ServiceType, reservation.ImplementationInstance!);
            }

            silo.Services.AddSingleton(TimeProvider.System);
        }));
        Assert.Equal(BuilderDiagnosticCodes.HostServicesChanged, Assert.Single(exception.Diagnostics).Code);
        Assert.DoesNotContain(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
        Assert.Equal(typeof(TimeProvider), Assert.Single(silo.Services).ServiceType);
        silo.UseMississippi(_ => { });
        Assert.Single(silo.Services, descriptor => descriptor.ServiceType == typeof(RuntimeAttachment));
    }

    /// <summary>
    ///     A shared service collection does not make a different configuration the owning silo context.
    /// </summary>
    [Fact]
    public void SharedServicesWithDifferentConfigurationAreRejected()
    {
        TestSiloBuilder silo = new();
        TestSiloBuilder different = new();
        ISiloBuilder wrapper = Mock.Of<ISiloBuilder>(candidate =>
            (candidate.Services == silo.Services) && (candidate.Configuration == different.Configuration));
        bool invoked = false;
        silo.UseMississippi(runtime =>
        {
            runtime.ConfigureSilo(_ => invoked = true);
            BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
                runtime.ApplyToSilo(wrapper));
            Assert.Equal(RuntimeBuilderDiagnosticCodes.SiloHostMismatch, Assert.Single(exception.Diagnostics).Code);
            Assert.False(invoked);
            runtime.ApplyToSilo(silo);
        });
        Assert.True(invoked);
    }

    /// <summary>
    ///     Catching a native callback failure cannot make its partial graph attachable.
    /// </summary>
    [Fact]
    public void SwallowedNativeFailureStillFailsTerminalValidation()
    {
        TestSiloBuilder silo = new();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(runtime =>
            {
                runtime.ConfigureSilo(staged =>
                {
                    staged.Services.AddSingleton("discarded");
                    throw new InvalidOperationException("Native configuration failed.");
                });
                Assert.Throws<InvalidOperationException>(() => runtime.ApplyToSilo(silo));
                Assert.Equal(
                    RuntimeBuilderDiagnosticCodes.SiloConfigurationFailed,
                    Assert.Single(runtime.Validate()).Code);
            }));
        Assert.Equal(RuntimeBuilderDiagnosticCodes.SiloConfigurationFailed, Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(silo.Services);
    }

    /// <summary>
    ///     Native callbacks run in order against staged services with the host configuration.
    /// </summary>
    [Fact]
    public void TerminalAttachmentAppliesQueuedNativeConfiguration()
    {
        TestSiloBuilder silo = new();
        List<int> order = [];
        ISiloBuilder? capturedSilo = null;
        silo.UseMississippi(runtime =>
        {
            Assert.Same(
                runtime,
                runtime.ConfigureSilo(staged =>
                {
                    capturedSilo = staged;
                    Assert.Same(silo.Configuration, staged.Configuration);
                    Assert.NotSame(silo.Services, staged.Services);
                    staged.Services.AddSingleton("configured");
                    order.Add(1);
                }));
            runtime.ConfigureSilo(staged =>
            {
                Assert.Contains(staged.Services, descriptor => descriptor.ServiceType == typeof(string));
                order.Add(2);
            });
            Assert.Empty(order);
        });
        Assert.Collection(order, first => Assert.Equal(1, first), second => Assert.Equal(2, second));
        Assert.NotNull(capturedSilo);
        Assert.True(capturedSilo.Services.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => capturedSilo.Services.AddSingleton(new object()));
        using ServiceProvider provider = silo.Services.BuildServiceProvider();
        Assert.Equal("configured", provider.GetRequiredService<string>());
    }
}