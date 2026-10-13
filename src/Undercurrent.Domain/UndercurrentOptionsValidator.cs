using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;

using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;


namespace Mississippi.Undercurrent.Domain;

/// <summary>Validates notification policy before a host accepts commands.</summary>
internal sealed class UndercurrentOptionsValidator : IValidateOptions<UndercurrentOptions>
{
    /// <summary>Gets the supported aggregate creation-input length budget.</summary>
    internal const int MaximumContentLengthBudget = 16_384;

    private static void ValidateLength(
        string name,
        int value,
        int minimum,
        List<string> failures
    )
    {
        if (value < minimum)
        {
            failures.Add($"{name} must be at least {minimum}.");
        }
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        UndercurrentOptions options
    )
    {
        if (options?.Notifications is not { } notifications)
        {
            return ValidateOptionsResult.Fail("Notification options are required.");
        }

        List<string> failures = [];
        if (notifications.MaxNotifications is <= 0 or > 10_000)
        {
            failures.Add("MaxNotifications must be between 1 and 10,000.");
        }

        if ((notifications.MaxCreationReceipts <= notifications.MaxNotifications) ||
            notifications.MaxCreationReceipts is <= 0 or > 100_000)
        {
            failures.Add("MaxCreationReceipts must exceed MaxNotifications and cannot exceed 100,000.");
        }

        if (notifications.MaxRequestLifetime <= TimeSpan.Zero)
        {
            failures.Add("MaxRequestLifetime must be positive.");
        }

        if (notifications.MaxSnapshotSizeBytes <= 0)
        {
            failures.Add("MaxSnapshotSizeBytes must be positive.");
        }

        ValidateLength(
            nameof(UserNotificationsOptions.MaxCreationKeyLength),
            notifications.MaxCreationKeyLength,
            1,
            failures);
        ValidateLength(nameof(UserNotificationsOptions.MaxTitleLength), notifications.MaxTitleLength, 1, failures);
        ValidateLength(
            nameof(UserNotificationsOptions.MaxDescriptionLength),
            notifications.MaxDescriptionLength,
            0,
            failures);
        ValidateLength(nameof(UserNotificationsOptions.MaxIconKeyLength), notifications.MaxIconKeyLength, 0, failures);
        ValidateLength(
            nameof(UserNotificationsOptions.MaxNavigationTargetLength),
            notifications.MaxNavigationTargetLength,
            0,
            failures);
        long contentBudget = (long)notifications.MaxCreationKeyLength +
                             notifications.MaxTitleLength +
                             notifications.MaxDescriptionLength +
                             notifications.MaxIconKeyLength +
                             notifications.MaxNavigationTargetLength;
        if (contentBudget > MaximumContentLengthBudget)
        {
            failures.Add("Combined character limits cannot exceed 16,384 UTF-16 code units.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}