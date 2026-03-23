using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Command handler that emits a single event carrying the large snapshot payload.
/// </summary>
internal sealed class StoreLargeSnapshotCommandHandler
    : CommandHandlerBase<StoreLargeSnapshotCommand, LargeSnapshotAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        StoreLargeSnapshotCommand command,
        LargeSnapshotAggregate? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Marker))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Marker must be provided.");
        }

        if (string.IsNullOrEmpty(command.Payload))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Payload must be provided.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new LargeSnapshotStored
                {
                    Marker = command.Marker,
                    Payload = command.Payload,
                },
            });
    }
}