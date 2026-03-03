using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Runtime.Abstractions;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Common.Builders.Runtime.L0Tests;

/// <summary>
///     L0 tests for <see cref="RuntimeBuilder" />.
/// </summary>
public sealed class RuntimeBuilderTests
{
    private sealed class TestProjectionState
    {
        public override string ToString() => nameof(TestProjectionState);
    }

    private sealed class TestSagaState
    {
        public override string ToString() => nameof(TestSagaState);
    }

    private sealed class TestSnapshot
    {
        public override string ToString() => nameof(TestSnapshot);
    }

    private sealed class TestSnapshotConverter
    {
        public override string ToString() => nameof(TestSnapshotConverter);
    }

    /// <summary>
    ///     AddAggregate returns a typed aggregate builder.
    /// </summary>
    [Fact]
    public void AddAggregateShouldReturnTypedAggregateBuilder()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        IAggregateBuilder<TestSnapshot> aggregateBuilder = builder.AddAggregate<TestSnapshot>();
        Assert.NotNull(aggregateBuilder);
    }

    /// <summary>
    ///     AddSaga returns a typed saga builder.
    /// </summary>
    [Fact]
    public void AddSagaShouldReturnTypedSagaBuilder()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        ISagaBuilder<TestSagaState> sagaBuilder = builder.AddSaga<TestSagaState>();
        Assert.NotNull(sagaBuilder);
    }

    /// <summary>
    ///     AddSiloConfiguration replays all configured silo actions during ApplyToSilo.
    /// </summary>
    [Fact]
    public void AddSiloConfigurationShouldReplayActionsDuringApplyToSilo()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        int calls = 0;
        builder.AddSiloConfiguration(_ => calls++);
        builder.AddSiloConfiguration(_ => calls++);
        Mock<ISiloBuilder> siloBuilder = new();
        _ = builder.ApplyToSilo(siloBuilder.Object);
        Assert.Equal(2, calls);
    }

    /// <summary>
    ///     AddSnapshotStateConverter registers the converter as transient.
    /// </summary>
    [Fact]
    public void AddSnapshotStateConverterShouldRegisterConverterAsTransient()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        IAggregateBuilder<TestSnapshot> aggregateBuilder = builder.AddAggregate<TestSnapshot>();
        aggregateBuilder.AddSnapshotStateConverter<TestSnapshotConverter>();
        ServiceDescriptor descriptor = Assert.Single(
            builder.Services,
            serviceDescriptor => serviceDescriptor.ServiceType == typeof(TestSnapshotConverter));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    /// <summary>
    ///     AddUxProjection returns a typed projection builder.
    /// </summary>
    [Fact]
    public void AddUxProjectionShouldReturnTypedProjectionBuilder()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        IUxProjectionBuilder<TestProjectionState> projectionBuilder = builder.AddUxProjection<TestProjectionState>();
        Assert.NotNull(projectionBuilder);
    }

    /// <summary>
    ///     ApplyToSilo returns the same builder and tracks invocation.
    /// </summary>
    [Fact]
    public void ApplyToSiloShouldReturnSameBuilderAndTrackInvocation()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        Mock<ISiloBuilder> siloBuilder = new();
        IRuntimeBuilder result = builder.ApplyToSilo(siloBuilder.Object);
        Assert.Same(builder, result);
        Assert.True(builder.SiloConfigurationApplied);
    }

    /// <summary>
    ///     ApplyToSilo throws when siloBuilder is null.
    /// </summary>
    [Fact]
    public void ApplyToSiloShouldThrowWhenSiloBuilderIsNull()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        ISiloBuilder? siloBuilder = null;
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder.ApplyToSilo(siloBuilder!));
        Assert.Equal("siloBuilder", exception.ParamName);
    }

    /// <summary>
    ///     ConfigureSnapshotRetention applies options configuration.
    /// </summary>
    [Fact]
    public void ConfigureSnapshotRetentionShouldConfigureOptions()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        builder.ConfigureSnapshotRetention(options =>
        {
            options.MaxSnapshotsToRetain = 3;
            options.SnapshotEveryNEvents = 50;
        });
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<SnapshotRetentionOptions> options = provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>();
        Assert.Equal(3, options.Value.MaxSnapshotsToRetain);
        Assert.Equal(50, options.Value.SnapshotEveryNEvents);
    }

    /// <summary>
    ///     ConfigureSnapshotRetention throws when configure delegate is null.
    /// </summary>
    [Fact]
    public void ConfigureSnapshotRetentionShouldThrowWhenConfigureIsNull()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        Action<SnapshotRetentionOptions>? configure = null;
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => builder.ConfigureSnapshotRetention(configure!));
        Assert.Equal("configure", exception.ParamName);
    }

    /// <summary>
    ///     Validate returns no diagnostics for runtime builders.
    /// </summary>
    [Fact]
    public void ValidateShouldReturnNoDiagnostics()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        builder.MarkFeatureConfigured("Runtime.Brooks");
        builder.MarkFeatureConfigured("Runtime.DomainModeling");
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        Assert.Empty(diagnostics);
    }

    /// <summary>
    ///     Validate returns required-feature diagnostics when runtime features are missing.
    /// </summary>
    [Fact]
    public void ValidateShouldReturnRequiredFeatureDiagnosticsWhenMissing()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostics, diagnostic => diagnostic.ErrorCode == "Runtime.BrooksNotConfigured");
        Assert.Contains(diagnostics, diagnostic => diagnostic.ErrorCode == "Runtime.DomainModelingNotConfigured");
    }
}