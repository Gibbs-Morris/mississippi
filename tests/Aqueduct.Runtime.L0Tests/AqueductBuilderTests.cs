using Mississippi.Aqueduct.Abstractions;
using Mississippi.Hosting.Abstractions;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>Verifies nested Aqueduct configuration and its closed-scope contract.</summary>
public sealed class AqueductBuilderTests
{
    /// <summary>Every configuration mutation rejects a closed nested scope.</summary>
    [Fact]
    public void ClosedScopeRejectsAllConfiguration()
    {
        AqueductBuilder builder = new();
        builder.Close();
        Assert.Equal(AqueductBuilderDiagnosticCodes.ConfigurationScopeClosed, Assert.Single(builder.Validate()).Code);
        Assert.Throws<BuilderValidationException>(() => builder.StreamProviderName = "late");
        Assert.Throws<BuilderValidationException>(() => builder.ServerStreamNamespace = "late");
        Assert.Throws<BuilderValidationException>(() => builder.AllClientsStreamNamespace = "late");
        Assert.Throws<BuilderValidationException>(() => builder.HeartbeatIntervalMinutes = 2);
        Assert.Throws<BuilderValidationException>(() => builder.DeadServerTimeoutMultiplier = 2);
        Assert.Throws<BuilderValidationException>(() => builder.UseMemoryStreams());
        Assert.Throws<BuilderValidationException>(() => builder.UseMemoryStreams("late"));
    }

    /// <summary>Default settings retain the existing backplane identities and timing.</summary>
    [Fact]
    public void DefaultsAreValid()
    {
        AqueductBuilder builder = new();
        Assert.Equal(AqueductStreamDefaults.StreamProviderName, builder.StreamProviderName);
        Assert.Equal(AqueductStreamDefaults.ServerStreamNamespace, builder.ServerStreamNamespace);
        Assert.Equal(AqueductStreamDefaults.AllClientsStreamNamespace, builder.AllClientsStreamNamespace);
        Assert.Equal(1, builder.HeartbeatIntervalMinutes);
        Assert.Equal(3, builder.DeadServerTimeoutMultiplier);
        Assert.Empty(builder.Validate());
    }

    /// <summary>Invalid names produce distinct, actionable diagnostics.</summary>
    /// <param name="value">An invalid name.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyNamesAreRejected(
        string? value
    )
    {
        AqueductBuilder builder = new()
        {
            StreamProviderName = value!,
            ServerStreamNamespace = value!,
            AllClientsStreamNamespace = value!,
        };
        Assert.Collection(
            builder.Validate(),
            diagnostic => Assert.Equal(AqueductBuilderDiagnosticCodes.StreamProviderRequired, diagnostic.Code),
            diagnostic => Assert.Equal(AqueductBuilderDiagnosticCodes.ServerNamespaceRequired, diagnostic.Code),
            diagnostic => Assert.Equal(AqueductBuilderDiagnosticCodes.BroadcastNamespaceRequired, diagnostic.Code));
    }

    /// <summary>Memory streams can select a provider without opening another configuration path.</summary>
    [Fact]
    public void MemoryStreamsKeepTheSelectedProvider()
    {
        AqueductBuilder builder = new();
        Assert.Same(builder, builder.UseMemoryStreams("custom"));
        Assert.Equal("custom", builder.StreamProviderName);
        Assert.Same(builder, builder.UseMemoryStreams());
    }

    /// <summary>Timing settings must be positive.</summary>
    /// <param name="value">An invalid interval or multiplier.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonpositiveTimingIsRejected(
        int value
    )
    {
        AqueductBuilder builder = new()
        {
            HeartbeatIntervalMinutes = value,
            DeadServerTimeoutMultiplier = value,
        };
        Assert.Collection(
            builder.Validate(),
            diagnostic => Assert.Equal(AqueductBuilderDiagnosticCodes.InvalidHeartbeatInterval, diagnostic.Code),
            diagnostic => Assert.Equal(AqueductBuilderDiagnosticCodes.InvalidTimeoutMultiplier, diagnostic.Code));
    }
}