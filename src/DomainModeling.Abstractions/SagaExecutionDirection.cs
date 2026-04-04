namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Identifies whether a saga execution attempt is moving forward or compensating prior work.
/// </summary>
public enum SagaExecutionDirection
{
    /// <summary>
    ///     The saga is executing its forward path.
    /// </summary>
    Forward,

    /// <summary>
    ///     The saga is compensating previously completed work.
    /// </summary>
    Compensation,
}