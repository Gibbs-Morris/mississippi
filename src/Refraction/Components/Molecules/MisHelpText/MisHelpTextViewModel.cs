namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a serializable view model for rendering a <see cref="MisHelpText" />.
/// </summary>
/// <remarks>
///     This type is public so state containers in consuming applications can define help text UI state
///     without depending on component internals.
/// </remarks>
public sealed record MisHelpTextViewModel
{
    /// <summary>
    ///     Gets a reusable default model instance.
    /// </summary>
    public static MisHelpTextViewModel Default { get; } = new();

    /// <summary>
    ///     Gets an optional additional CSS class for the help text.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    ///     Gets the id attribute value for the element.
    /// </summary>
    public string? Id { get; init; }
}