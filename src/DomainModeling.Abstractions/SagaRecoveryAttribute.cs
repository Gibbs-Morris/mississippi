using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Configures framework-owned recovery behavior for a saga state type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SagaRecoveryAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryAttribute" /> class.
    /// </summary>
    /// <param name="mode">The saga recovery mode.</param>
    public SagaRecoveryAttribute(
        SagaRecoveryMode mode = SagaRecoveryMode.Automatic
    )
    {
        Mode = mode;
    }

    /// <summary>
    ///     Gets the configured saga recovery mode.
    /// </summary>
    public SagaRecoveryMode Mode { get; }

    /// <summary>
    ///     Gets the optional runtime recovery profile name.
    /// </summary>
    public string? Profile { get; init; }
}
