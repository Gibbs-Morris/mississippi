// <copyright file="Channels.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Server.Services;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Pages;

/// <summary>
///     Page component for viewing and interacting with channels.
/// </summary>
public sealed partial class Channels : ComponentBase
{
    /// <summary>
    ///     Gets or sets the selected channel ID from the route.
    /// </summary>
    [Parameter]
    public string? ChannelId { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private UserSession UserSession { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!UserSession.IsAuthenticated)
        {
            Navigation.NavigateTo("/login");
        }
    }

    private void HandleChannelSelected(
        string channelId
    )
    {
        Navigation.NavigateTo($"/channels/{channelId}");
    }
}