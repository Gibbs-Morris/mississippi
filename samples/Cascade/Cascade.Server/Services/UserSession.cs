using System;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.Domain.User;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Server.Services;

/// <summary>
///     Scoped service that stores the current user's session state.
/// </summary>
/// <remarks>
///     <para>
///         This service is scoped per Blazor circuit, meaning each browser tab
///         has its own session. It stores the user's identity after login.
///     </para>
///     <para>
///         Note: This is a simplified session for the sample application.
///         In production, use ASP.NET Core authentication and authorization.
///     </para>
/// </remarks>
internal sealed class UserSession : IUserContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserSession" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    public UserSession(
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
        AggregateGrainFactory = aggregateGrainFactory ?? throw new ArgumentNullException(nameof(aggregateGrainFactory));

    /// <summary>
    ///     Occurs when the session state changes.
    /// </summary>
    public event EventHandler? OnChanged;

    /// <summary>
    ///     Gets or sets the user's display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    /// <summary>
    ///     Gets or sets the user's unique identifier.
    /// </summary>
    public string? UserId { get; set; }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <summary>
    ///     Clears the session (logout without setting offline).
    /// </summary>
    public void Clear()
    {
        UserId = null;
        DisplayName = null;
        OnChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Logs in the user by registering them (or finding existing) via the aggregate.
    /// </summary>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the login operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when registration fails unexpectedly.</exception>
    public async Task LoginAsync(
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(displayName);

        // Generate userId from display name (simplified for demo)
        // Using a predictable format to allow re-login with same name
        string normalizedName = displayName.ToUpperInvariant().Replace(" ", "-", StringComparison.Ordinal);
        string userId = $"user-{normalizedName}";

        // Get the user aggregate grain
        AggregateKey aggregateKey = AggregateKey.ForAggregate<IUserAggregateGrain>(userId);
        IUserAggregateGrain userGrain = AggregateGrainFactory.GetAggregate<IUserAggregateGrain>(aggregateKey);

        // Register the user (idempotent - returns error if already registered, which we ignore)
        OperationResult result = await userGrain.RegisterAsync(userId, displayName);

        // Allow "already registered" error (InvalidState) - that's fine for login
        if (!result.Success && (result.ErrorCode != AggregateErrorCodes.InvalidState))
        {
            throw new InvalidOperationException(result.ErrorMessage);
        }

        // Set user online
        await userGrain.SetOnlineStatusAsync(true);

        // Store in session
        UserId = userId;
        DisplayName = displayName;
        OnChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Logs out the user and sets them offline.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the logout operation.</returns>
    public async Task LogoutAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrEmpty(UserId))
        {
            // Set user offline
            AggregateKey aggregateKey = AggregateKey.ForAggregate<IUserAggregateGrain>(UserId);
            IUserAggregateGrain userGrain = AggregateGrainFactory.GetAggregate<IUserAggregateGrain>(aggregateKey);
            await userGrain.SetOnlineStatusAsync(false);
        }

        UserId = null;
        DisplayName = null;
        OnChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Sets the user identity after login.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="displayName">The user's display name.</param>
    public void SetUser(
        string userId,
        string displayName
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(displayName);
        UserId = userId;
        DisplayName = displayName;
        OnChanged?.Invoke(this, EventArgs.Empty);
    }
}