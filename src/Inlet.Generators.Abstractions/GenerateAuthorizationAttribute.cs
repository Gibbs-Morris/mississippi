using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Emits an ASP.NET Core <c>[Authorize]</c> attribute on generated HTTP APIs.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to aggregate, command, projection, or saga types that participate
///         in source-generated HTTP API surfaces.
///     </para>
///     <para>
///         Generated output uses standard ASP.NET Core authorization metadata and runtime behavior.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateAuthorizationAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the comma-delimited authentication schemes.
    /// </summary>
    public string? AuthenticationSchemes { get; set; }

    /// <summary>
    ///     Gets or sets the authorization policy name.
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    ///     Gets or sets the comma-delimited roles list.
    /// </summary>
    public string? Roles { get; set; }
}