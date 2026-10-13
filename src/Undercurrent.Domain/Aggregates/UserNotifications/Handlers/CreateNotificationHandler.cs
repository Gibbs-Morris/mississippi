using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;

/// <summary>Creates notification identities and enforces bounded, fixed-deadline retry protection.</summary>
internal sealed class CreateNotificationHandler : CommandHandlerBase<CreateNotification, UserNotificationsAggregate>
{
    /// <summary>Initializes a new instance of the <see cref="CreateNotificationHandler" /> class.</summary>
    /// <param name="timeProvider">The server clock.</param>
    /// <param name="policy">The validated notification policy.</param>
    public CreateNotificationHandler(
        TimeProvider timeProvider,
        UserNotificationPolicy policy
    )
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    private UserNotificationPolicy Policy { get; }

    private TimeProvider TimeProvider { get; }

    private static OperationResult<IReadOnlyList<object>> Fail(
        string code,
        string message
    ) =>
        OperationResult.Fail<IReadOnlyList<object>>(code, message);

    private static bool IsValidUtf16(
        string? value
    )
    {
        ReadOnlySpan<char> remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out Rune _, out int consumed) != OperationStatus.Done)
            {
                return false;
            }

            remaining = remaining[consumed..];
        }

        return true;
    }

    private static OperationResult<IReadOnlyList<object>>? ValidateShape(
        CreateNotification command
    )
    {
        if (string.IsNullOrWhiteSpace(command.CreationKey))
        {
            return Fail(AggregateErrorCodes.InvalidCommand, "Creation key is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Fail(AggregateErrorCodes.InvalidCommand, "Title is required.");
        }

        long length = (long)command.CreationKey.Length +
                      command.Title.Length +
                      (command.Description?.Length ?? 0) +
                      (command.IconKey?.Length ?? 0) +
                      (command.NavigationTarget?.Length ?? 0);
        if (length > UndercurrentOptionsValidator.MaximumContentLengthBudget)
        {
            return Fail(
                AggregateErrorCodes.InvalidCommand,
                "Creation request content exceeds the supported length budget.");
        }

        string?[] values =
            [command.CreationKey, command.Title, command.Description, command.IconKey, command.NavigationTarget];
        return values.All(IsValidUtf16)
            ? null
            : Fail(AggregateErrorCodes.InvalidCommand, "Creation request content must contain valid UTF-16.");
    }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        CreateNotification command,
        UserNotificationsAggregate? state
    )
    {
        if (ValidateShape(command) is { } invalid)
        {
            return invalid;
        }

        string fingerprint = CreationRequestFingerprint.Create(command);
        DateTimeOffset expiresAt = command.ExpiresAt.ToUniversalTime();
        if (state is not null &&
            state.CreationReceipts.TryGetValue(command.CreationKey, out NotificationCreationReceipt? receipt))
        {
            if ((receipt.ExpiresAt != expiresAt) ||
                !string.Equals(receipt.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return Fail(AggregateErrorCodes.InvalidState, "Creation key already identifies a different request.");
            }

            return state.Notifications.ContainsKey(receipt.NotificationId)
                ? OperationResult.Ok<IReadOnlyList<object>>([])
                : Fail(AggregateErrorCodes.InvalidState, "Notification has already been deleted.");
        }

        if (ValidatePolicy(command) is { } outsidePolicy)
        {
            return outsidePolicy;
        }

        DateTimeOffset now = TimeProvider.GetUtcNow().ToUniversalTime();
        DateTimeOffset cutoff = state?.ExpiredRequestsThrough is { } previous && (previous > now) ? previous : now;
        if (expiresAt <= cutoff)
        {
            return Fail(AggregateErrorCodes.InvalidCommand, "Creation request has expired.");
        }

        if ((expiresAt - now) > Policy.MaxRequestLifetime)
        {
            return Fail(AggregateErrorCodes.InvalidCommand, "Creation request expiry exceeds the maximum lifetime.");
        }

        int evictionCount = Math.Max(0, ((state?.Notifications.Count ?? 0) - Policy.MaxNotifications) + 1);
        ImmutableArray<string> evictedIds = state?.NotificationOrder.Take(evictionCount).ToImmutableArray() ?? [];
        if (evictedIds.Length != evictionCount)
        {
            return Fail(AggregateErrorCodes.InvalidState, "Notification order is inconsistent.");
        }

        ImmutableDictionary<string, UserNotification> retained = state?.Notifications.RemoveRange(evictedIds) ??
                                                                 ImmutableDictionary<string, UserNotification>.Empty;
        int survivingReceipts = state?.CreationReceipts.Count(pair =>
                                    (pair.Value.ExpiresAt > cutoff) ||
                                    retained.ContainsKey(pair.Value.NotificationId)) ??
                                0;
        if (survivingReceipts >= Policy.MaxCreationReceipts)
        {
            return Fail(AggregateErrorCodes.InvalidState, "Creation receipt capacity is exhausted.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new NotificationCreated
            {
                Notification = new()
                {
                    NotificationId = Guid.NewGuid().ToString("D"),
                    Title = command.Title,
                    Description = command.Description,
                    IconKey = command.IconKey,
                    NavigationTarget = command.NavigationTarget,
                    CreatedAt = now,
                },
                CreationKey = command.CreationKey,
                ExpiresAt = expiresAt,
                RequestFingerprint = fingerprint,
                EvictedNotificationIds = evictedIds,
                ExpiredRequestsThrough = cutoff,
            },
        ]);
    }

    private OperationResult<IReadOnlyList<object>>? ValidatePolicy(
        CreateNotification command
    )
    {
        (string Name, int Length, int Maximum)[] limits =
        [
            ("Creation key", command.CreationKey.Length, Policy.MaxCreationKeyLength),
            ("Title", command.Title.Length, Policy.MaxTitleLength),
            ("Description", command.Description?.Length ?? 0, Policy.MaxDescriptionLength),
            ("Icon key", command.IconKey?.Length ?? 0, Policy.MaxIconKeyLength),
            ("Target URL", command.NavigationTarget?.Length ?? 0, Policy.MaxNavigationTargetLength),
        ];
        foreach ((string name, int length, int maximum) in limits)
        {
            if (length > maximum)
            {
                return Fail(AggregateErrorCodes.InvalidCommand, name + " exceeds the configured maximum length.");
            }
        }

        return null;
    }
}