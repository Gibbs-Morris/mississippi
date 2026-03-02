using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Builders.Runtime.Abstractions;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Common.Builders.Runtime.L0Tests;

/// <summary>
///     L0 tests for <see cref="RuntimeBuilder" />.
/// </summary>
public sealed class RuntimeBuilderTests
{
    [Fact]
    public void ApplyToSiloShouldReturnSameBuilderAndTrackInvocation()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        Mock<ISiloBuilder> siloBuilder = new();
        IRuntimeBuilder result = builder.ApplyToSilo(siloBuilder.Object);
        Assert.Same(builder, result);
        Assert.True(builder.SiloConfigurationApplied);
    }

    [Fact]
    public void ApplyToSiloShouldThrowWhenSiloBuilderIsNull()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        ISiloBuilder? siloBuilder = null;
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder.ApplyToSilo(siloBuilder!));
        Assert.Equal("siloBuilder", exception.ParamName);
    }

    [Fact]
    public void ConfigureSnapshotRetentionShouldConfigureOptions()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        builder.ConfigureSnapshotRetention(options =>
        {
            options.MaxSnapshotsToRetain = 3;
            options.SnapshotEveryNEvents = 50;
        });
        ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<SnapshotRetentionOptions> options = provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>();
        Assert.Equal(3, options.Value.MaxSnapshotsToRetain);
        Assert.Equal(50, options.Value.SnapshotEveryNEvents);
    }

    [Fact]
    public void ConfigureSnapshotRetentionShouldThrowWhenConfigureIsNull()
    {
        RuntimeBuilder builder = RuntimeBuilder.Create();
        Action<SnapshotRetentionOptions>? configure = null;
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => builder.ConfigureSnapshotRetention(configure!));
        Assert.Equal("configure", exception.ParamName);
    }
}