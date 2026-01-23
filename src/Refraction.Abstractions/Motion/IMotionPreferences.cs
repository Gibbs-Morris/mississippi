namespace Mississippi.Refraction.Abstractions.Motion;

/// <summary>
///     Provides user motion/animation preferences for Refraction components.
/// </summary>
/// <remarks>
///     Implementations respect user accessibility settings (prefers-reduced-motion)
///     and allow runtime configuration of animation behavior.
/// </remarks>
public interface IMotionPreferences
{
    /// <summary>
    ///     Gets the duration multiplier for animations.
    /// </summary>
    /// <remarks>
    ///     Default is 1.0. Values less than 1.0 speed up animations;
    ///     values greater than 1.0 slow them down. When <see cref="ReduceMotion" />
    ///     is true, this should return 0.
    /// </remarks>
    double DurationMultiplier { get; }

    /// <summary>
    ///     Gets a value indicating whether entrance animations should play.
    /// </summary>
    bool EnableEntranceAnimations { get; }

    /// <summary>
    ///     Gets a value indicating whether exit animations should play.
    /// </summary>
    bool EnableExitAnimations { get; }

    /// <summary>
    ///     Gets a value indicating whether animations should be reduced or disabled.
    /// </summary>
    /// <remarks>
    ///     When true, components should skip or minimize animations
    ///     to respect user accessibility preferences.
    /// </remarks>
    bool ReduceMotion { get; }
}