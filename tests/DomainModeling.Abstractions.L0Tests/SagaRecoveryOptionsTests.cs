using System;


namespace Mississippi.DomainModeling.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRecoveryOptions" />.
/// </summary>
public sealed class SagaRecoveryOptionsTests
{
    /// <summary>
    ///     Verifies the default saga recovery options values.
    /// </summary>
    [Fact]
    public void ConstructorSetsExpectedDefaults()
    {
        SagaRecoveryOptions options = new();
        Assert.True(options.Enabled);
        Assert.False(options.ForceManualOnly);
        Assert.Equal(TimeSpan.FromMinutes(1), options.InitialReminderDueTime);
        Assert.Equal(10, options.MaxAutomaticAttempts);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ReminderPeriod);
    }

    /// <summary>
    ///     Verifies configured values are preserved.
    /// </summary>
    [Fact]
    public void InitPropertiesPreserveConfiguredValues()
    {
        SagaRecoveryOptions options = new()
        {
            Enabled = false,
            ForceManualOnly = true,
            InitialReminderDueTime = TimeSpan.FromSeconds(30),
            MaxAutomaticAttempts = 3,
            ReminderPeriod = TimeSpan.FromMinutes(2),
        };
        Assert.False(options.Enabled);
        Assert.True(options.ForceManualOnly);
        Assert.Equal(TimeSpan.FromSeconds(30), options.InitialReminderDueTime);
        Assert.Equal(3, options.MaxAutomaticAttempts);
        Assert.Equal(TimeSpan.FromMinutes(2), options.ReminderPeriod);
    }
}