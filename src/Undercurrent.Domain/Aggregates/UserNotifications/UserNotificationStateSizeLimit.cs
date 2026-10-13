using System;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;

/// <summary>Checks candidate inbox state with the same converter used for persisted snapshots.</summary>
internal sealed class UserNotificationStateSizeLimit
{
    /// <summary>Initializes a new instance of the <see cref="UserNotificationStateSizeLimit" /> class.</summary>
    /// <param name="policy">The captured host policy.</param>
    /// <param name="converter">The registered snapshot converter.</param>
    public UserNotificationStateSizeLimit(
        UserNotificationPolicy policy,
        ISnapshotStateConverter<UserNotificationsAggregate> converter
    )
    {
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    private ISnapshotStateConverter<UserNotificationsAggregate> Converter { get; }

    private UserNotificationPolicy Policy { get; }

    /// <summary>Determines whether the candidate's actual serialized payload fits the configured limit.</summary>
    /// <param name="state">The complete candidate state.</param>
    /// <returns>Whether the payload can be accepted.</returns>
    public bool Fits(
        UserNotificationsAggregate state
    ) =>
        Converter.ToEnvelope(state, string.Empty).Data.Length <= Policy.MaxSnapshotSizeBytes;
}