namespace Mississippi.Refraction.Abstractions.Theme;

/// <summary>
///     Specifies how Refraction should resolve motion-sensitive rendering.
/// </summary>
public enum RefractionMotionMode
{
    /// <summary>
    ///     Resolves motion from the current preference snapshot.
    /// </summary>
    System,

    /// <summary>
    ///     Uses the standard motion presentation.
    /// </summary>
    Standard,

    /// <summary>
    ///     Uses the reduced-motion presentation.
    /// </summary>
    Reduced,
}