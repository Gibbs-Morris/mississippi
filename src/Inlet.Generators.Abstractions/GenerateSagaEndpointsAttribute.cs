using System;

namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a saga for infrastructure code generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to a saga state record, generators will produce server endpoints,
///         client actions/state, and silo registrations for saga orchestration.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateSagaEndpointsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the saga input payload type.
    /// </summary>
    public Type? InputType { get; set; }

    /// <summary>
    ///     Gets or sets the feature key for client-side state management.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to camelCase of the saga name without the "SagaState" suffix.
    ///     </para>
    /// </remarks>
    public string? FeatureKey { get; set; }

    /// <summary>
    ///     Gets or sets the route prefix for saga endpoints.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to kebab-case of the saga name without the "SagaState" suffix.
    ///     </para>
    ///     <para>
    ///         The full route pattern is: <c>api/sagas/{RoutePrefix}/{sagaId}</c>.
    ///     </para>
    /// </remarks>
    public string? RoutePrefix { get; set; }
}
