using System;
using System.Collections.Concurrent;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

internal enum ReplicaSinkExecutionBlockKind
{
    Throttled = 0,
    Parked = 1,
    Quarantined = 2,
}

internal sealed class ReplicaSinkExecutionBlock
{
    public ReplicaSinkExecutionBlock(
        ReplicaSinkExecutionBlockKind kind,
        string failureCode,
        string failureSummary,
        DateTimeOffset? untilUtc = null
    )
    {
        ArgumentNullException.ThrowIfNull(failureCode);
        ArgumentNullException.ThrowIfNull(failureSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureSummary);
        Kind = kind;
        FailureCode = failureCode;
        FailureSummary = failureSummary;
        UntilUtc = untilUtc;
    }

    public string FailureCode { get; }

    public string FailureSummary { get; }

    public ReplicaSinkExecutionBlockKind Kind { get; }

    public DateTimeOffset? UntilUtc { get; }
}

internal interface IReplicaSinkExecutionHealthManager
{
    ReplicaSinkExecutionBlock? GetCurrentBlock(
        string sinkKey
    );

    bool IsBlocked(
        string sinkKey
    );

    void Park(
        string sinkKey,
        string failureCode,
        string failureSummary,
        DateTimeOffset untilUtc
    );

    void Quarantine(
        string sinkKey,
        string failureCode,
        string failureSummary
    );

    void Throttle(
        string sinkKey,
        string failureCode,
        string failureSummary,
        DateTimeOffset untilUtc
    );
}

internal sealed class ReplicaSinkExecutionHealthManager : IReplicaSinkExecutionHealthManager
{
    public ReplicaSinkExecutionHealthManager(
        TimeProvider timeProvider
    ) =>
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private ConcurrentDictionary<string, ReplicaSinkExecutionBlock> Blocks { get; } = new(StringComparer.Ordinal);

    private TimeProvider TimeProvider { get; }

    public ReplicaSinkExecutionBlock? GetCurrentBlock(
        string sinkKey
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        if (!Blocks.TryGetValue(sinkKey, out ReplicaSinkExecutionBlock? block))
        {
            return null;
        }

        if (block.UntilUtc is not null && block.UntilUtc.Value <= TimeProvider.GetUtcNow())
        {
            _ = Blocks.TryRemove(sinkKey, out _);
            return null;
        }

        return block;
    }

    public bool IsBlocked(
        string sinkKey
    ) =>
        GetCurrentBlock(sinkKey) is not null;

    public void Park(
        string sinkKey,
        string failureCode,
        string failureSummary,
        DateTimeOffset untilUtc
    ) =>
        SetBlock(sinkKey, new(ReplicaSinkExecutionBlockKind.Parked, failureCode, failureSummary, untilUtc));

    public void Quarantine(
        string sinkKey,
        string failureCode,
        string failureSummary
    ) =>
        SetBlock(sinkKey, new(ReplicaSinkExecutionBlockKind.Quarantined, failureCode, failureSummary));

    public void Throttle(
        string sinkKey,
        string failureCode,
        string failureSummary,
        DateTimeOffset untilUtc
    ) =>
        SetBlock(sinkKey, new(ReplicaSinkExecutionBlockKind.Throttled, failureCode, failureSummary, untilUtc));

    private void SetBlock(
        string sinkKey,
        ReplicaSinkExecutionBlock block
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentNullException.ThrowIfNull(block);
        Blocks.AddOrUpdate(
            sinkKey,
            block,
            (
                _,
                existingBlock
            ) =>
            {
                if (existingBlock.Kind == ReplicaSinkExecutionBlockKind.Quarantined)
                {
                    return existingBlock;
                }

                if (block.Kind == ReplicaSinkExecutionBlockKind.Quarantined)
                {
                    return block;
                }

                if (block.Kind > existingBlock.Kind)
                {
                    return block;
                }

                if (block.Kind < existingBlock.Kind)
                {
                    return existingBlock;
                }

                if (existingBlock.UntilUtc is null)
                {
                    return existingBlock;
                }

                if (block.UntilUtc is null)
                {
                    return block;
                }

                return block.UntilUtc.Value >= existingBlock.UntilUtc.Value ? block : existingBlock;
            });
    }
}
