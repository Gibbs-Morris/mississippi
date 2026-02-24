using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Emits an ASP.NET Core <c>[AllowAnonymous]</c> attribute on generated HTTP APIs.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to aggregate, command, projection, or saga types that participate
///         in source-generated HTTP API surfaces.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateAllowAnonymousAttribute : Attribute;