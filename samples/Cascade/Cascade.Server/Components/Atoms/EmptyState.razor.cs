// <copyright file="EmptyState.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Atoms;

/// <summary>
///     Atom component for empty state display.
/// </summary>
public sealed partial class EmptyState : ComponentBase
{
    /// <summary>
    ///     Gets or sets the empty state message to display.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Nothing to display";
}