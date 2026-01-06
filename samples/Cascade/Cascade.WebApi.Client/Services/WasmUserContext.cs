using System;
using System.Threading.Tasks;

using Cascade.Components.Services;

using Microsoft.JSInterop;


namespace Cascade.WebApi.Client.Services;

/// <summary>
///     WASM implementation of <see cref="IUserContext"/> backed by browser local storage.
/// </summary>
internal sealed class WasmUserContext : IUserContext
{
    private readonly IJSRuntime jsRuntime;

    private string? cachedDisplayName;

    private string? cachedUserId;

    private bool isInitialized;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmUserContext"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime for accessing browser storage.</param>
    public WasmUserContext(
        IJSRuntime jsRuntime
    )
    {
        this.jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <inheritdoc/>
    public string? DisplayName => cachedDisplayName;

    /// <inheritdoc/>
    public bool IsAuthenticated => !string.IsNullOrEmpty(cachedUserId);

    /// <inheritdoc/>
    public string? UserId => cachedUserId;

    /// <summary>
    ///     Initializes the user context from local storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            cachedUserId = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "cascade_userId");
            cachedDisplayName = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "cascade_displayName");
        }
        catch (JSException)
        {
            // Ignore JS errors during pre-rendering
        }

        isInitialized = true;
    }

    /// <summary>
    ///     Logs in the user with the specified credentials.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="displayName">The display name.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoginAsync(
        string userId,
        string displayName
    )
    {
        cachedUserId = userId;
        cachedDisplayName = displayName;

        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "cascade_userId", userId);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "cascade_displayName", displayName);
    }

    /// <summary>
    ///     Logs out the current user.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogoutAsync()
    {
        cachedUserId = null;
        cachedDisplayName = null;

        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "cascade_userId");
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "cascade_displayName");
    }
}
