using System;
using System.Collections.Generic;
using System.Linq;

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
        Assert.Equal("MSB002", Assert.Single(captured.Validate()).Code);
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
            Assert.Equal("MSB101", Assert.Single(wrongHost.Diagnostics).Code);
            Assert.Empty(other.Services);
            runtime.ApplyToSilo(silo);
            BuilderValidationException duplicate = Assert.Throws<BuilderValidationException>(() =>
                runtime.ApplyToSilo(silo));
            Assert.Equal("MSB102", Assert.Single(duplicate.Diagnostics).Code);
            BuilderValidationException lateConfiguration = Assert.Throws<BuilderValidationException>(() =>
                runtime.ConfigureSilo(_ => { }));
            Assert.Equal("MSB104", Assert.Single(lateConfiguration.Diagnostics).Code);
        });
        other.UseMississippi(_ => { });
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
        Assert.Equal("MSB103", Assert.Single(exception.Diagnostics).Code);
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