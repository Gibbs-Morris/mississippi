using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Identifies the runtime operator making an administrative request.
/// </summary>
public sealed class ReplicaSinkOperatorContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkOperatorContext" /> class.
    /// </summary>
    /// <param name="actorId">The stable operator identity for audit trails.</param>
    /// <param name="accessLevel">The operator access level.</param>
    public ReplicaSinkOperatorContext(
        string actorId,
        ReplicaSinkOperatorAccessLevel accessLevel
    )
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ActorId = actorId;
        AccessLevel = accessLevel;
    }

    /// <summary>
    ///     Gets the operator access level.
    /// </summary>
    public ReplicaSinkOperatorAccessLevel AccessLevel { get; }

    /// <summary>
    ///     Gets the stable operator identity for audit trails.
    /// </summary>
    public string ActorId { get; }
}
