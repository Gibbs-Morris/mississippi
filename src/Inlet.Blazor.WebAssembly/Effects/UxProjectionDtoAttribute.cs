using System;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Marks a DTO record as a client-side representation of a UX projection.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to a client-side DTO class to link it to the
///         corresponding server-side projection via the route string. The route
///         must match the route specified in the server's <c>[UxProjection("route")]</c>
///         attribute.
///     </para>
///     <para>
///         The <see cref="IProjectionDtoRegistry" /> scans assemblies for types
///         decorated with this attribute and builds a mapping from route strings
///         to DTO types.
///     </para>
///     <para>
///         Example usage:
///         <code>
///             // Server-side (in Domain project with Orleans):
///             [UxProjection("channel-messages")]
///             public sealed record ChannelMessagesProjection { ... }
///
///             // Client-side (in Contracts or Client project, WASM-safe):
///             [UxProjectionDto("channel-messages")]
///             public sealed record ChannelMessagesDto { ... }
///         </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UxProjectionDtoAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionDtoAttribute" /> class.
    /// </summary>
    /// <param name="route">
    ///     The route segment that links this DTO to the server projection.
    ///     Must match the route in the corresponding <c>[UxProjection("route")]</c> attribute.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route" /> is null.</exception>
    public UxProjectionDtoAttribute(
        string route
    )
    {
        ArgumentNullException.ThrowIfNull(route);
        Route = route;
    }

    /// <summary>
    ///     Gets the route segment for this projection DTO.
    /// </summary>
    /// <remarks>
    ///     This route is used to:
    ///     <list type="bullet">
    ///         <item>Construct the HTTP endpoint URL: <c>/api/projections/{route}/{entityId}</c></item>
    ///         <item>Match SignalR update notifications to the correct DTO type.</item>
    ///     </list>
    /// </remarks>
    public string Route { get; }
}
