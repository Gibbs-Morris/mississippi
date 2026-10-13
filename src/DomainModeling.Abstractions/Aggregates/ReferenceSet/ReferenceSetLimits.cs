namespace Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet;

/// <summary>Defines the fixed bounds of the native reference-set primitive.</summary>
/// <remarks>Public so applications can present the same bounds before submitting commands.</remarks>
public static class ReferenceSetLimits
{
    /// <summary>The maximum number of members in a collection.</summary>
    public const int MaximumMembers = 1000;

    /// <summary>The maximum source identifier length in UTF-16 code units.</summary>
    public const int MaximumSourceIdLength = 256;
}