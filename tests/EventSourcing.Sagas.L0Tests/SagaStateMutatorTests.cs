using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaStateMutator" />.
/// </summary>
public sealed class SagaStateMutatorTests
{
    /// <summary>
    ///     Verifies CreateUpdated copies existing values and applies updates.
    /// </summary>
    [Fact]
    public void CreateUpdatedCopiesValuesAndAppliesUpdate()
    {
        TestSagaState initial = new()
        {
            Name = "Alpha",
            Phase = SagaPhase.Running,
            StepHash = "HASH",
        };
        TestSagaState updated = SagaStateMutator.CreateUpdated(
            initial,
            static (
                map,
                state
            ) => map.SetProperty(state, nameof(TestSagaState.Name), "Beta"));
        Assert.Equal("Beta", updated.Name);
        Assert.Equal(SagaPhase.Running, updated.Phase);
        Assert.Equal("HASH", updated.StepHash);
    }

    /// <summary>
    ///     Verifies CreateUpdated handles null state by creating a new instance.
    /// </summary>
    [Fact]
    public void CreateUpdatedCreatesNewInstanceWhenStateNull()
    {
        TestSagaState updated = SagaStateMutator.CreateUpdated<TestSagaState>(
            null,
            static (
                map,
                state
            ) => map.SetProperty(state, nameof(TestSagaState.Name), "Gamma"));
        Assert.Equal("Gamma", updated.Name);
        Assert.Equal(SagaPhase.NotStarted, updated.Phase);
    }

    /// <summary>
    ///     Verifies CreateUpdated throws when saga state lacks a parameterless constructor.
    /// </summary>
    [Fact]
    public void CreateUpdatedThrowsWhenNoParameterlessCtor()
    {
        Assert.Throws<MissingMethodException>(() => SagaStateMutator.CreateUpdated<SagaStateWithoutParameterlessCtor>(
            null,
            static (
                map,
                state
            ) => map.SetProperty(state, nameof(SagaStateWithoutParameterlessCtor.Name), "Delta")));
    }

    /// <summary>
    ///     Verifies CreateUpdated throws for null update action.
    /// </summary>
    [Fact]
    public void CreateUpdatedThrowsWhenUpdateNull()
    {
        Assert.Throws<ArgumentNullException>(() => SagaStateMutator.CreateUpdated<TestSagaState>(null, null!));
    }
}