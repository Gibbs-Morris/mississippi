using System;

using Mississippi.EventSourcing.Sagas.Abstractions.Commands;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Commands;

/// <summary>
///     Tests for saga command records to ensure proper instantiation and property access.
/// </summary>
public sealed class SagaCommandRecordsTests
{
    /// <summary>
    ///     Test input record for StartSagaCommand tests.
    /// </summary>
    private sealed record TestSagaInput(string OrderName, decimal Amount);

    /// <summary>
    ///     CancelSagaCommand can be created with reason.
    /// </summary>
    [Fact]
    public void CancelSagaCommandShouldExposeReason()
    {
        // Arrange
        string reason = "User requested cancellation";

        // Act
        CancelSagaCommand cmd = new(reason);

        // Assert
        Assert.Equal(reason, cmd.Reason);
    }

    /// <summary>
    ///     CancelSagaCommand implements record equality.
    /// </summary>
    [Fact]
    public void CancelSagaCommandShouldImplementRecordEquality()
    {
        // Arrange
        CancelSagaCommand cmd1 = new("Timeout");
        CancelSagaCommand cmd2 = new("Timeout");

        // Assert
        Assert.Equal(cmd1, cmd2);
    }

    /// <summary>
    ///     ExecuteNextStepCommand can be instantiated.
    /// </summary>
    [Fact]
    public void ExecuteNextStepCommandShouldBeInstantiable()
    {
        // Arrange & Act
        ExecuteNextStepCommand cmd = new();

        // Assert
        Assert.NotNull(cmd);
    }

    /// <summary>
    ///     ExecuteNextStepCommand implements record equality.
    /// </summary>
    [Fact]
    public void ExecuteNextStepCommandShouldImplementRecordEquality()
    {
        // Arrange
        ExecuteNextStepCommand cmd1 = new();
        ExecuteNextStepCommand cmd2 = new();

        // Assert
        Assert.Equal(cmd1, cmd2);
    }

    /// <summary>
    ///     StartSagaCommand can be created with input and correlation ID.
    /// </summary>
    [Fact]
    public void StartSagaCommandShouldExposeInputAndCorrelationId()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        TestSagaInput input = new("Test Order", 100m);
        string correlationId = "corr-123";

        // Act
        StartSagaCommand<TestSagaInput> cmd = new(sagaId, input, correlationId);

        // Assert
        Assert.Equal(sagaId, cmd.SagaId);
        Assert.Equal(input, cmd.Input);
        Assert.Equal(correlationId, cmd.CorrelationId);
    }

    /// <summary>
    ///     StartSagaCommand implements record equality.
    /// </summary>
    [Fact]
    public void StartSagaCommandShouldImplementRecordEquality()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        TestSagaInput input = new("Order", 25m);
        StartSagaCommand<TestSagaInput> cmd1 = new(sagaId, input, "corr");
        StartSagaCommand<TestSagaInput> cmd2 = new(sagaId, input, "corr");

        // Assert
        Assert.Equal(cmd1, cmd2);
    }

    /// <summary>
    ///     StartSagaCommand can be created without correlation ID.
    /// </summary>
    [Fact]
    public void StartSagaCommandShouldSupportNullCorrelationId()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        TestSagaInput input = new("Test Order", 50m);

        // Act
        StartSagaCommand<TestSagaInput> cmd = new(sagaId, input);

        // Assert
        Assert.Equal(sagaId, cmd.SagaId);
        Assert.Equal(input, cmd.Input);
        Assert.Null(cmd.CorrelationId);
    }
}