using System;

using Microsoft.Extensions.Options;

using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;

/// <summary>Captures validated host policy so a command uses one immutable set of limits.</summary>
internal sealed class UserNotificationPolicy
{
    /// <summary>Initializes a new instance of the <see cref="UserNotificationPolicy" /> class.</summary>
    /// <param name="options">The host options.</param>
    public UserNotificationPolicy(
        IOptions<UndercurrentOptions> options
    )
    {
        UndercurrentOptions configured = options?.Value ?? throw new ArgumentNullException(nameof(options));
        ValidateOptionsResult validation = new UndercurrentOptionsValidator().Validate(Options.DefaultName, configured);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(UndercurrentOptions), validation.Failures);
        }

        UserNotificationsOptions source = configured.Notifications;
        MaxNotifications = source.MaxNotifications;
        MaxCreationReceipts = source.MaxCreationReceipts;
        MaxRequestLifetime = source.MaxRequestLifetime;
        MaxCreationKeyLength = source.MaxCreationKeyLength;
        MaxTitleLength = source.MaxTitleLength;
        MaxDescriptionLength = source.MaxDescriptionLength;
        MaxIconKeyLength = source.MaxIconKeyLength;
        MaxNavigationTargetLength = source.MaxNavigationTargetLength;
    }

    /// <summary>Gets the creation key length limit.</summary>
    public int MaxCreationKeyLength { get; }

    /// <summary>Gets the receipt capacity.</summary>
    public int MaxCreationReceipts { get; }

    /// <summary>Gets the description length limit.</summary>
    public int MaxDescriptionLength { get; }

    /// <summary>Gets the icon key length limit.</summary>
    public int MaxIconKeyLength { get; }

    /// <summary>Gets the navigation target length limit.</summary>
    public int MaxNavigationTargetLength { get; }

    /// <summary>Gets the retained notification capacity.</summary>
    public int MaxNotifications { get; }

    /// <summary>Gets the maximum future request lifetime.</summary>
    public TimeSpan MaxRequestLifetime { get; }

    /// <summary>Gets the title length limit.</summary>
    public int MaxTitleLength { get; }
}