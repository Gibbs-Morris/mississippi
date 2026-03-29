namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Specifies how Refraction should resolve contrast-sensitive rendering.
/// </summary>
public enum RefractionContrastMode
{
    /// <summary>
    ///     Resolves contrast from the current preference snapshot.
    /// </summary>
    System,

    /// <summary>
    ///     Uses the standard contrast presentation.
    /// </summary>
    Standard,

    /// <summary>
    ///     Uses the high-contrast presentation.
    /// </summary>
    High,
}