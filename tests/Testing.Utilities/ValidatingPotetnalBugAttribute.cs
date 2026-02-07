using System;


namespace Mississippi.Testing.Utilities;

/// <summary>
///     Marks a test method as one that validates a potential bug in the codebase.
///     Tests decorated with this attribute demonstrate behavior that is believed
///     to be incorrect or unintended. These tests are intentionally written to
///     surface, not fix, the identified issue.
/// </summary>
/// <remarks>
///     <para>
///         Use this attribute on unit test methods that reproduce a suspected defect.
///         The attribute carries metadata describing the bug so that it can be triaged
///         and addressed separately.
///     </para>
///     <para>
///         Tests with this attribute may intentionally assert incorrect behavior
///         (the bug). When the underlying code is fixed, these tests are expected
///         to fail and should be updated to assert the corrected behavior.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ValidatingPotetnalBugAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidatingPotetnalBugAttribute" /> class.
    /// </summary>
    /// <param name="description">A description of the potential bug.</param>
    public ValidatingPotetnalBugAttribute(
        string description
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = description;
    }

    /// <summary>
    ///     Gets a description of the potential bug being validated.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets or sets the source file path where the bug exists.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    ///     Gets or sets the line number(s) in the source file where the bug is located.
    /// </summary>
    public string? LineNumbers { get; set; }

    /// <summary>
    ///     Gets or sets an optional severity indicator (e.g., "Low", "Medium", "High", "Critical").
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    ///     Gets or sets the category of the bug (e.g., "ThreadSafety", "LogicError", "ResourceLeak").
    /// </summary>
    public string? Category { get; set; }
}
