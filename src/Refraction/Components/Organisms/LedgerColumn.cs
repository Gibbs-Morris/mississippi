// <copyright file="LedgerColumn.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// Defines a column in the Ledger component.
/// </summary>
public class LedgerColumn
{
    /// <summary>
    /// Gets or sets the column header text.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this column contains numeric data.
    /// </summary>
    public bool IsNumeric { get; set; }

    /// <summary>
    /// Gets or sets the column width (CSS value).
    /// </summary>
    public string? Width { get; set; }
}
