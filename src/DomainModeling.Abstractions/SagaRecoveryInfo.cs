using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes saga-level recovery metadata resolved for runtime use.
/// </summary>
public sealed record SagaRecoveryInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryInfo" /> class.
    /// </summary>
    /// <param name="mode">The saga recovery mode.</param>
    /// <param name="profile">The optional recovery profile name.</param>
    public SagaRecoveryInfo(
        SagaRecoveryMode mode,
        string? profile
    )
    {
        Mode = mode;
        Profile = string.IsNullOrWhiteSpace(profile)
            ? null
            : profile;
    }

    /// <summary>
    ///     Gets the configured saga recovery mode.
    /// </summary>
    public SagaRecoveryMode Mode { get; }

    /// <summary>
    ///     Gets the optional recovery profile name.
    /// </summary>
    public string? Profile { get; }
}
