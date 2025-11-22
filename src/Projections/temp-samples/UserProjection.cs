using System;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Simple user projection record used by the temp samples to demonstrate reducer behavior.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="Email">The user's primary email address.</param>
/// <param name="DisplayName">The friendly display name to render in UIs.</param>
/// <param name="IsActive">Indicates whether the user is active.</param>
/// <param name="RegisteredAt">When the user was registered.</param>
/// <param name="DeactivatedAt">When the user was deactivated, if applicable.</param>
public sealed record UserProjection(
    Guid UserId,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? DeactivatedAt
)
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserProjection" /> class with default values.
    /// </summary>
    public UserProjection()
        : this(Guid.Empty, string.Empty, string.Empty, false, DateTimeOffset.MinValue, null)
    {
    }

    /// <summary>
    ///     Gets the safe default instance so reducers always work with a non-null projection.
    /// </summary>
    public static UserProjection Empty { get; } = new();

    /// <summary>
    ///     Returns the supplied projection or the <see cref="Empty" /> instance when null.
    /// </summary>
    /// <param name="projection">The current projection instance.</param>
    /// <returns>A non-null projection.</returns>
    public static UserProjection Ensure(
        UserProjection? projection
    ) =>
        projection ?? Empty;
}