namespace Cascade.Components.Services;

/// <summary>
///     Provides access to the current user context.
/// </summary>
/// <remarks>
///     <para>
///         This abstraction allows shared components to access user identity
///         without depending on host-specific authentication mechanisms.
///     </para>
///     <para>
///         Implementations:
///         <list type="bullet">
///             <item>
///                 <term>Blazor Server</term>
///                 <description>Backed by <c>UserSession</c> with circuit-scoped state.</description>
///             </item>
///             <item>
///                 <term>Blazor WASM</term>
///                 <description>Backed by browser local storage or API-provided claims.</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public interface IUserContext
{
    /// <summary>
    ///     Gets the current user's identifier.
    /// </summary>
    /// <value>The user identifier if authenticated; otherwise, <c>null</c>.</value>
    string? UserId { get; }

    /// <summary>
    ///     Gets the current user's display name.
    /// </summary>
    /// <value>The display name if authenticated; otherwise, <c>null</c>.</value>
    string? DisplayName { get; }

    /// <summary>
    ///     Gets a value indicating whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
