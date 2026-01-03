// <copyright file="OnlineIndicator.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Atoms;

/// <summary>
///     Atom component for showing online/offline status.
/// </summary>
public sealed partial class OnlineIndicator : ComponentBase
{
    /// <summary>
    ///     Gets or sets a value indicating whether the user is online.
    /// </summary>
    [Parameter]
    public bool IsOnline { get; set; }
}