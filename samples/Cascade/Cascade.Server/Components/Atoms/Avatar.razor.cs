// <copyright file="Avatar.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Atoms;

/// <summary>
///     Atom component for displaying user avatars.
/// </summary>
public sealed partial class Avatar : ComponentBase
{
    /// <summary>
    ///     Gets or sets the user name to display initials for.
    /// </summary>
    [Parameter]
    public string UserName { get; set; } = string.Empty;
}