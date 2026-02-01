// <copyright file="DataGridEnums.cs" company="Batch Enterprises">
// Copyright (c) Batch Enterprises. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Specifies the size variant for the DataGrid.
/// </summary>
public enum DataGridSize
{
    /// <summary>
    /// Compact row height for dense data.
    /// </summary>
    Compact,

    /// <summary>
    /// Default row height.
    /// </summary>
    Default,

    /// <summary>
    /// Comfortable row height with more padding.
    /// </summary>
    Comfortable,
}

/// <summary>
/// Specifies the selection mode for the DataGrid.
/// </summary>
public enum DataGridSelectionMode
{
    /// <summary>
    /// No row selection.
    /// </summary>
    None,

    /// <summary>
    /// Single row selection.
    /// </summary>
    OneRow,

    /// <summary>
    /// Multiple row selection.
    /// </summary>
    Multiple,
}

/// <summary>
/// Specifies the sort direction for a column.
/// </summary>
public enum DataGridSortDirection
{
    /// <summary>
    /// No sorting applied.
    /// </summary>
    None,

    /// <summary>
    /// Ascending order.
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending order.
    /// </summary>
    Descending,
}
