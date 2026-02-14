namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Defines the supported HTML input types for <see cref="MisTextInput" />.
/// </summary>
public enum MisTextInputType
{
    /// <summary>
    ///     A standard single-line text input.
    /// </summary>
    Text,

    /// <summary>
    ///     An input optimized for email addresses.
    /// </summary>
    Email,

    /// <summary>
    ///     A password input.
    /// </summary>
    Password,

    /// <summary>
    ///     A search input.
    /// </summary>
    Search,

    /// <summary>
    ///     A telephone number input.
    /// </summary>
    Tel,

    /// <summary>
    ///     A URL input.
    /// </summary>
    Url,
}