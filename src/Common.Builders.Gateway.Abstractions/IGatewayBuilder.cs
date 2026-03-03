using Mississippi.Common.Builders.Abstractions;


namespace Mississippi.Common.Builders.Gateway.Abstractions;

/// <summary>
///     Contract for configuring Mississippi gateway host composition.
/// </summary>
public interface IGatewayBuilder : IMississippiBuilder
{
    /// <summary>
    ///     Marks this gateway builder as explicitly anonymous.
    /// </summary>
    /// <returns>The same builder instance for fluent chaining.</returns>
    IGatewayBuilder AllowAnonymousExplicitly();

    /// <summary>
    ///     Marks this gateway builder as authorization-configured.
    /// </summary>
    /// <returns>The same builder instance for fluent chaining.</returns>
    IGatewayBuilder ConfigureAuthorization();
}