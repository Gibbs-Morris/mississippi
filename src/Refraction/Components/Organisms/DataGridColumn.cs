// <copyright file="DataGridColumn.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

using System;

using Microsoft.AspNetCore.Components;

namespace Refraction.Components.Organisms;

/// <summary>
/// Defines a column in the DataGrid.
/// </summary>
/// <typeparam name="TItem">The type of data item.</typeparam>
public class DataGridColumn<TItem>
{
    /// <summary>
    /// Gets or sets the column header text.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name for sorting.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets a function to get the property value.
    /// </summary>
    public Func<TItem, object?>? PropertySelector { get; set; }

    /// <summary>
    /// Gets or sets a custom template for the cell.
    /// </summary>
    public RenderFragment<TItem>? Template { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is sortable.
    /// </summary>
    public bool IsSortable { get; set; }

    /// <summary>
    /// Gets or sets the column width.
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Gets or sets the column minimum width.
    /// </summary>
    public string? MinWidth { get; set; }
}
