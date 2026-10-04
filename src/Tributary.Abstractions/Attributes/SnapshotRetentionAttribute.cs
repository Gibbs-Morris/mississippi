using System;


namespace Mississippi.Tributary.Abstractions.Attributes;

/// <summary>
///     Sets the snapshot persistence interval for a state class.
/// </summary>
/// <remarks>
///     Apply this attribute to a state class that carries a <c>SnapshotStorageName</c>.
///     The modulus selects snapshot positions divisible by the configured value.
///     Configuration overrides take precedence over this attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SnapshotRetentionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotRetentionAttribute" /> class.
    /// </summary>
    /// <param name="modulus">The positive interval between persisted snapshot positions.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="modulus" /> is not positive.</exception>
    public SnapshotRetentionAttribute(
        int modulus
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modulus);
        Modulus = modulus;
    }

    /// <summary>
    ///     Gets the interval between persisted snapshot positions.
    /// </summary>
    public int Modulus { get; }
}