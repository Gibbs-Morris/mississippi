using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a manually implemented type or member as a candidate for future
///     Roslyn source generation.
/// </summary>
/// <remarks>
///     <para>
///         Use this attribute to tag code that follows a pattern suitable for
///         automation. The code is implemented by hand first to validate the
///         pattern, then the attribute signals that a source generator should
///         eventually produce this code automatically.
///     </para>
///     <para>
///         When migrating to a generator, search for usages of this attribute
///         to identify all manual implementations that need replacement.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         [PendingSourceGenerator("AggregateService generator should produce this mapper")]
///         public sealed class UserAggregateMapper : IMapper&lt;UserAggregate, UserDto&gt;
///         {
///             // Hand-crafted implementation that establishes the pattern
///         }
///     </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Property,
    Inherited = false)]
public sealed class PendingSourceGeneratorAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PendingSourceGeneratorAttribute" /> class.
    /// </summary>
    public PendingSourceGeneratorAttribute()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PendingSourceGeneratorAttribute" /> class
    ///     with a reason describing the intended generator behavior.
    /// </summary>
    /// <param name="reason">
    ///     A description of what the source generator should produce or
    ///     which generator pattern this code should follow.
    /// </param>
    public PendingSourceGeneratorAttribute(
        string reason
    ) =>
        Reason = reason;

    /// <summary>
    ///     Gets a description of the intended source generator behavior.
    /// </summary>
    /// <value>
    ///     A human-readable explanation of what the generator should produce
    ///     or which pattern this code exemplifies.
    /// </value>
    public string? Reason { get; }
}