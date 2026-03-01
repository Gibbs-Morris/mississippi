using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Emits ASP.NET Core <c>[AllowAnonymous]</c> metadata for generated HTTP APIs and projection subscriptions.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to aggregate, command, projection, or saga types that participate
///         in source-generated HTTP API surfaces.
///     </para>
///     <para>
///         Projection types are also consumed at runtime by Inlet projection assembly scanning to
///         control subscription authorization behavior for SignalR-based projection subscriptions.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateAllowAnonymousAttribute : Attribute;