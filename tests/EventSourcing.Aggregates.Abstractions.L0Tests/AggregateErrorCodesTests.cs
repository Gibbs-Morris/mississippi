

namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateErrorCodes" /> constants.
/// </summary>
public class AggregateErrorCodesTests
{
    /// <summary>
    ///     AlreadyExists should have the expected value.
    /// </summary>
    [Fact]
    public void AlreadyExistsHasExpectedValue()
    {
        Assert.Equal("AGGREGATE_ALREADY_EXISTS", AggregateErrorCodes.AlreadyExists);
    }

    /// <summary>
    ///     CommandHandlerNotFound should have the expected value.
    /// </summary>
    [Fact]
    public void CommandHandlerNotFoundHasExpectedValue()
    {
        Assert.Equal("COMMAND_HANDLER_NOT_FOUND", AggregateErrorCodes.CommandHandlerNotFound);
    }

    /// <summary>
    ///     ConcurrencyConflict should have the expected value.
    /// </summary>
    [Fact]
    public void ConcurrencyConflictHasExpectedValue()
    {
        Assert.Equal("CONCURRENCY_CONFLICT", AggregateErrorCodes.ConcurrencyConflict);
    }

    /// <summary>
    ///     InvalidCommand should have the expected value.
    /// </summary>
    [Fact]
    public void InvalidCommandHasExpectedValue()
    {
        Assert.Equal("INVALID_COMMAND", AggregateErrorCodes.InvalidCommand);
    }

    /// <summary>
    ///     InvalidState should have the expected value.
    /// </summary>
    [Fact]
    public void InvalidStateHasExpectedValue()
    {
        Assert.Equal("INVALID_STATE", AggregateErrorCodes.InvalidState);
    }

    /// <summary>
    ///     NotFound should have the expected value.
    /// </summary>
    [Fact]
    public void NotFoundHasExpectedValue()
    {
        Assert.Equal("AGGREGATE_NOT_FOUND", AggregateErrorCodes.NotFound);
    }
}