// <copyright file="LoadingSpinner.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Atoms;

/// <summary>
///     Atom component for loading state indication.
/// </summary>
public sealed partial class LoadingSpinner : ComponentBase
{
    /// <summary>
    ///     Gets or sets the loading message to display.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Loading...";
}