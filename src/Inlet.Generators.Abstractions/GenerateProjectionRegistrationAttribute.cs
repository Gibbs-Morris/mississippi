using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Selects silo registration generation for a projection.
/// </summary>
/// <remarks>
///     <para>
///         The projection and domain silo registration generators consume this marker.
///         The composition root must call the generated registration helper.
///     </para>
///     <para>
///         HTTP endpoints and DTOs require <see cref="GenerateProjectionEndpointsAttribute" />
///         independently. Runtime-only projections do not require a projection path.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateProjectionRegistrationAttribute : Attribute
{
}