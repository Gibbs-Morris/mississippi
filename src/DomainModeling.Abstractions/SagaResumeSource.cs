namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Identifies what triggered a saga execution attempt.
/// </summary>
public enum SagaResumeSource
{
    /// <summary>
    ///     The execution is part of the initial orchestration flow.
    /// </summary>
    Initial,

    /// <summary>
    ///     The execution was triggered by an Orleans reminder.
    /// </summary>
    Reminder,

    /// <summary>
    ///     The execution was triggered manually by an operator.
    /// </summary>
    Manual,
}