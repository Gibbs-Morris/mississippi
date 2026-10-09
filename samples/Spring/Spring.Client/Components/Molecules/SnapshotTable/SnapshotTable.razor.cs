using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace MississippiSamples.Spring.Client.Components.Molecules;

/// <summary>
///     Displays a labelled, keyboard-scrollable snapshot without reading application state.
/// </summary>
/// <remarks>Public so Spring pages can compose this presentational molecule in Razor markup.</remarks>
public sealed partial class SnapshotTable
{
    /// <summary>Gets or sets the property and value rows.</summary>
    [Parameter]
    public IReadOnlyList<(string Name, string Value)> Rows { get; set; } = [];

    /// <summary>Gets or sets the visible table title.</summary>
    [Parameter]
    public string Title { get; set; } = "Snapshot";
}