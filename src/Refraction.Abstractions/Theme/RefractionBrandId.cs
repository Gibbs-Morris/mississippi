using System;


namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Identifies a host-selectable Refraction brand.
/// </summary>
public readonly record struct RefractionBrandId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RefractionBrandId" /> struct.
    /// </summary>
    /// <param name="value">The host-supplied brand identifier.</param>
    public RefractionBrandId(
        string value
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    ///     Gets the canonical brand identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Returns the canonical string representation of the brand identifier.
    /// </summary>
    /// <returns>The canonical brand identifier.</returns>
    public override string ToString() => Value;
}