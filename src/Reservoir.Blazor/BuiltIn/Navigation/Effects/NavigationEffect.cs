using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Effects;

/// <summary>
///     Effect that handles navigation actions by calling NavigationManager or JS interop.
/// </summary>
/// <remarks>
///     <para>
///         This effect responds to navigation intent actions (<see cref="NavigateAction" />,
///         <see cref="ReplaceRouteAction" />, <see cref="SetQueryParamsAction" />,
///         <see cref="ScrollToAnchorAction" />, <see cref="OpenExternalLinkAction" />)
///         by invoking the appropriate <see cref="NavigationManager" /> methods or JS interop.
///     </para>
///     <para>
///         The effect does not emit any actions; the <see cref="Components.ReservoirNavigationProvider" />
///         component handles dispatching <see cref="LocationChangedAction" /> when navigation completes.
///     </para>
/// </remarks>
public sealed class NavigationEffect : IActionEffect<NavigationState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NavigationEffect" /> class.
    /// </summary>
    /// <param name="navigationManager">The Blazor navigation manager.</param>
    /// <param name="jsRuntime">The JavaScript runtime for opening external links.</param>
    /// <exception cref="ArgumentNullException">Thrown if navigationManager or jsRuntime is null.</exception>
    public NavigationEffect(
        NavigationManager navigationManager,
        IJSRuntime jsRuntime
    )
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        NavigationManager = navigationManager;
        JSRuntime = jsRuntime;
    }

    private IJSRuntime JSRuntime { get; }

    private NavigationManager NavigationManager { get; }

    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    ) =>
        action is NavigateAction or ReplaceRouteAction or SetQueryParamsAction or ScrollToAnchorAction or OpenExternalLinkAction;

    /// <inheritdoc />
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        NavigationState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        switch (action)
        {
            case NavigateAction navigate:
                HandleNavigate(navigate);
                break;
            case ReplaceRouteAction replace:
                HandleReplace(replace);
                break;
            case SetQueryParamsAction setParams:
                HandleSetQueryParams(setParams);
                break;
            case ScrollToAnchorAction scrollToAnchor:
                HandleScrollToAnchor(scrollToAnchor);
                break;
            case OpenExternalLinkAction openExternal:
                await HandleOpenExternalLinkAsync(openExternal, cancellationToken).ConfigureAwait(false);
                break;
        }

        // No actions to emit - LocationChangedAction is dispatched by ReservoirNavigationProvider
        yield break;
    }

    private void HandleNavigate(
        NavigateAction action
    ) =>
        NavigationManager.NavigateTo(action.Uri, action.ForceLoad);

    private async Task HandleOpenExternalLinkAsync(
        OpenExternalLinkAction action,
        CancellationToken cancellationToken
    ) =>
        await JSRuntime.InvokeVoidAsync("open", cancellationToken, action.Url, "_blank").ConfigureAwait(false);

    private void HandleReplace(
        ReplaceRouteAction action
    ) =>
        NavigationManager.NavigateTo(
            action.Uri,
            new NavigationOptions
            {
                ForceLoad = action.ForceLoad,
                ReplaceHistoryEntry = true,
            });

    private void HandleScrollToAnchor(
        ScrollToAnchorAction action
    )
    {
        // Get the current path without fragment
        string currentUri = NavigationManager.Uri;
        int fragmentIndex = currentUri.IndexOf('#', StringComparison.Ordinal);
        string basePath = fragmentIndex >= 0 ? currentUri[..fragmentIndex] : currentUri;

        // Navigate to the anchor
        string targetUri = $"{basePath}#{action.AnchorId}";
        if (action.ReplaceHistory)
        {
            NavigationManager.NavigateTo(
                targetUri,
                new NavigationOptions
                {
                    ReplaceHistoryEntry = true,
                });
        }
        else
        {
            NavigationManager.NavigateTo(targetUri);
        }
    }

    private void HandleSetQueryParams(
        SetQueryParamsAction action
    )
    {
        // Build the new URI with updated query parameters
        string newUri = NavigationManager.GetUriWithQueryParameters(
            action.Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        if (action.ReplaceHistory)
        {
            NavigationManager.NavigateTo(
                newUri,
                new NavigationOptions
                {
                    ReplaceHistoryEntry = true,
                });
        }
        else
        {
            NavigationManager.NavigateTo(newUri);
        }
    }
}