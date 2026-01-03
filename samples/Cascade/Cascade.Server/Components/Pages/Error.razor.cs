// <copyright file="Error.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Diagnostics;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;


namespace Cascade.Server.Components.Pages;

/// <summary>
///     Page component for displaying error information.
/// </summary>
public sealed partial class Error : ComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
    }
}