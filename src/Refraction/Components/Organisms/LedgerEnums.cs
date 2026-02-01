// <copyright file="LedgerEnums.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

namespace Refraction.Components.Organisms;

/// <summary>
/// State variants for the Ledger component.
/// </summary>
public enum LedgerState
{
    /// <summary>
    /// Ledger is collapsed, showing minimal rows.
    /// </summary>
    Collapsed,

    /// <summary>
    /// Ledger is fully expanded.
    /// </summary>
    Expanded,
}

/// <summary>
/// Size variants for the Ledger component.
/// </summary>
public enum LedgerSize
{
    /// <summary>
    /// Compact ledger with smaller text.
    /// </summary>
    Compact,

    /// <summary>
    /// Standard ledger size.
    /// </summary>
    Default,

    /// <summary>
    /// Comfortable ledger with more spacing.
    /// </summary>
    Comfortable,
}
