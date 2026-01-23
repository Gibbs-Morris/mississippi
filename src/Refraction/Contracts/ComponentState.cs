namespace Mississippi.Refraction.Contracts;

/// <summary>
///     Type-safe component state that converts to string for CSS data-attribute binding.
/// </summary>
/// <remarks>
///     <para>
///         This struct provides compile-time safety for component states while maintaining
///         CSS compatibility through implicit string conversion.
///     </para>
///     <para>
///         Use the static properties for standard states. Use <see cref="Custom" /> for
///         enterprise-specific states that extend the design system.
///     </para>
/// </remarks>
public readonly record struct ComponentState
{
    private readonly string value;

    private ComponentState(
        string value
    ) =>
        this.value = value;

    /// <summary>Gets the active state for components accepting input.</summary>
    public static ComponentState Active { get; } = new(RefractionStates.Active);

    /// <summary>Gets the busy state for components that are processing.</summary>
    public static ComponentState Busy { get; } = new(RefractionStates.Busy);

    /// <summary>Gets the disabled state for non-interactive components.</summary>
    public static ComponentState Disabled { get; } = new(RefractionStates.Disabled);

    /// <summary>Gets the error state for components in an error condition.</summary>
    public static ComponentState Error { get; } = new(RefractionStates.Error);

    /// <summary>Gets the expanded state for components in expanded view.</summary>
    public static ComponentState Expanded { get; } = new(RefractionStates.Expanded);

    /// <summary>Gets the focused state for components with focus.</summary>
    public static ComponentState Focused { get; } = new(RefractionStates.Focused);

    /// <summary>Gets the default idle state.</summary>
    public static ComponentState Idle { get; } = new(RefractionStates.Idle);

    /// <summary>Gets the latent state for components hidden until intent.</summary>
    public static ComponentState Latent { get; } = new(RefractionStates.Latent);

    /// <summary>Gets the locked state for components frozen during transition.</summary>
    public static ComponentState Locked { get; } = new(RefractionStates.Locked);

    /// <summary>
    ///     Creates a custom state for enterprise extensions.
    /// </summary>
    /// <param name="value">The custom state value.</param>
    /// <returns>A new component state with the specified value.</returns>
    public static ComponentState Custom(
        string value
    ) =>
        new(value);

    /// <summary>
    ///     Implicitly converts a component state to string for CSS binding.
    /// </summary>
    /// <param name="state">The component state.</param>
    public static implicit operator string(
        ComponentState state
    ) =>
        state.value;

    /// <inheritdoc />
    public override string ToString() => value;
}