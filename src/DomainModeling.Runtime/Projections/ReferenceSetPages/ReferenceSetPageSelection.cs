using System;


namespace Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

/// <summary>Validates selection at both application and grain read boundaries.</summary>
internal static class ReferenceSetPageSelection
{
    /// <summary>Rejects selections outside the fixed runtime limits.</summary>
    /// <param name="offset">The requested offset.</param>
    /// <param name="limit">The requested limit.</param>
    public static void Validate(
        int offset,
        int limit
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
    }
}