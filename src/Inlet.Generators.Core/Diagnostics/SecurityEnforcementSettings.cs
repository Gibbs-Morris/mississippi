using System;
using System.Collections.Generic;
using System.Linq;


namespace Mississippi.Inlet.Generators.Core.Diagnostics;

/// <summary>
///     Represents security enforcement settings from [RequireSecureEndpoints].
/// </summary>
public sealed class SecurityEnforcementSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SecurityEnforcementSettings" /> class.
    /// </summary>
    /// <param name="treatAnonymousAsError">Whether anonymous endpoints should be errors.</param>
    /// <param name="exemptTypes">List of type names exempt from security enforcement.</param>
    public SecurityEnforcementSettings(
        bool treatAnonymousAsError,
        IReadOnlyList<string> exemptTypes
    )
    {
        TreatAnonymousAsError = treatAnonymousAsError;
        ExemptTypes = exemptTypes ?? throw new ArgumentNullException(nameof(exemptTypes));
    }

    /// <summary>
    ///     Gets the list of type names exempt from security enforcement.
    /// </summary>
    public IReadOnlyList<string> ExemptTypes { get; }

    /// <summary>
    ///     Gets a value indicating whether anonymous endpoints should be treated as errors.
    /// </summary>
    public bool TreatAnonymousAsError { get; }

    /// <summary>
    ///     Checks if a type is exempt from security enforcement.
    /// </summary>
    /// <param name="fullyQualifiedTypeName">The fully qualified type name.</param>
    /// <returns>True if the type is exempt.</returns>
    public bool IsTypeExempt(
        string fullyQualifiedTypeName
    )
    {
        if (string.IsNullOrEmpty(fullyQualifiedTypeName))
        {
            return false;
        }

        return ExemptTypes.Any(exemptType => fullyQualifiedTypeName.EndsWith(exemptType, StringComparison.Ordinal) ||
                                             (fullyQualifiedTypeName == exemptType));
    }
}