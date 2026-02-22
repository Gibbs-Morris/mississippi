using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaInputProvidedReducer{TSaga,TInput}" />.
/// </summary>
public sealed class SagaInputProvidedReducerTests
{
    private sealed record InputSagaState : ISagaState
    {
        public string? CorrelationId { get; init; }

        public TestInput? Input { get; init; }

        public int LastCompletedStepIndex { get; init; }

        public string Name { get; init; } = string.Empty;

        public SagaPhase Phase { get; init; }

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }
    }

    private sealed record MismatchedInputSagaState : ISagaState
    {
        public string? CorrelationId { get; init; }

        public string? Input { get; init; }

        public int LastCompletedStepIndex { get; init; }

        public string Name { get; init; } = string.Empty;

        public SagaPhase Phase { get; init; }

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }
    }

    private sealed record TestInput(string TransferId);

    private sealed record ValueTypeInputSagaState : ISagaState
    {
        public string? CorrelationId { get; init; }

        public int Input { get; init; }

        public int LastCompletedStepIndex { get; init; }

        public string Name { get; init; } = string.Empty;

        public SagaPhase Phase { get; init; }

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }
    }

    /// <summary>
    ///     Verifies the reducer ignores incompatible Input property types.
    /// </summary>
    [Fact]
    public void ReduceIgnoresIncompatibleInputProperty()
    {
        SagaInputProvidedReducer<MismatchedInputSagaState, TestInput> reducer = new();
        SagaInputProvided<TestInput> @event = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-7"),
        };
        MismatchedInputSagaState initial = new()
        {
            Name = "Gamma",
            Input = "existing",
        };
        MismatchedInputSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal("existing", updated.Input);
        Assert.Equal("Gamma", updated.Name);
    }

    /// <summary>
    ///     Verifies the reducer tolerates missing Input property.
    /// </summary>
    [Fact]
    public void ReduceIgnoresMissingInputProperty()
    {
        SagaInputProvidedReducer<TestSagaState, TestInput> reducer = new();
        SagaInputProvided<TestInput> @event = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
        };
        TestSagaState initial = new()
        {
            Name = "Beta",
            Phase = SagaPhase.Running,
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(initial.Name, updated.Name);
        Assert.Equal(initial.Phase, updated.Phase);
    }

    /// <summary>
    ///     Verifies null input is ignored for non-nullable value types.
    /// </summary>
    [Fact]
    public void ReduceIgnoresNullInputForNonNullableValueType()
    {
        SagaInputProvidedReducer<ValueTypeInputSagaState, int?> reducer = new();
        ValueTypeInputSagaState initial = new()
        {
            Name = "Epsilon",
            Input = 17,
        };
        SagaInputProvided<int?> @event = new()
        {
            SagaId = Guid.NewGuid(),
            Input = null,
        };
        ValueTypeInputSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(17, updated.Input);
        Assert.Equal("Epsilon", updated.Name);
    }

    /// <summary>
    ///     Verifies the reducer sets the Input property when present and compatible.
    /// </summary>
    [Fact]
    public void ReduceSetsInputWhenPropertyExists()
    {
        SagaInputProvidedReducer<InputSagaState, TestInput> reducer = new();
        TestInput input = new("transfer-42");
        SagaInputProvided<TestInput> @event = new()
        {
            SagaId = Guid.NewGuid(),
            Input = input,
        };
        InputSagaState initial = new()
        {
            Name = "Alpha",
        };
        InputSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(input, updated.Input);
        Assert.Equal("Alpha", updated.Name);
    }

    /// <summary>
    ///     Verifies the reducer sets a reference input to null when provided.
    /// </summary>
    [Fact]
    public void ReduceSetsNullInputWhenReferenceType()
    {
        SagaInputProvidedReducer<InputSagaState, TestInput?> reducer = new();
        InputSagaState initial = new()
        {
            Name = "Delta",
            Input = new("transfer-9"),
        };
        SagaInputProvided<TestInput?> @event = new()
        {
            SagaId = Guid.NewGuid(),
            Input = null,
        };
        InputSagaState updated = reducer.Reduce(initial, @event);
        Assert.Null(updated.Input);
        Assert.Equal("Delta", updated.Name);
    }
}