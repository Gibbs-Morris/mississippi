namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Describes the caller access level for the runtime operator surface.
/// </summary>
public enum ReplicaSinkOperatorAccessLevel
{
    /// <summary>
    ///     The caller can read summary dead-letter metadata only.
    /// </summary>
    Summary = 0,

    /// <summary>
    ///     The caller can read detailed dead-letter failure summaries.
    /// </summary>
    Detail = 1,

    /// <summary>
    ///     The caller can perform administrative actions such as controlled re-drive.
    /// </summary>
    Admin = 2,
}
