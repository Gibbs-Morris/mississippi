using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Triggers generation of a composite Inlet client registration that
///     registers all aggregate features, Reservoir built-ins, and SignalR setup.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an assembly, the <c>InletClientCompositeGenerator</c> will:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Discover all aggregates (via commands marked with <see cref="GenerateCommandAttribute" />)
///                 and call their generated <c>Add{Aggregate}AggregateFeature()</c> methods.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Register Reservoir Blazor built-ins via <c>AddReservoirBlazorBuiltIns()</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Configure Inlet client with SignalR and automatic projection DTO scanning.
///             </description>
///         </item>
///     </list>
///     <para>
///         Example usage:
///         <code>
///         [assembly: GenerateInletClientComposite(AppName = "Spring")]
///         </code>
///         This generates <c>SpringInletRegistrations.AddSpringInlet()</c> which can replace
///         multiple manual registration calls in Program.cs.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GenerateInletClientCompositeAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the application name used in generated type and method names.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For example, <c>AppName = "Spring"</c> generates:
    ///         <list type="bullet">
    ///             <item><c>SpringInletRegistrations</c> (class name)</item>
    ///             <item><c>AddSpringInlet()</c> (extension method for MississippiClientBuilder)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public string? AppName { get; set; }

    /// <summary>
    ///     Gets or sets the SignalR hub path for Inlet real-time updates.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>/hubs/inlet</c>, which is the conventional path used across Mississippi samples.
    /// </remarks>
    public string HubPath { get; set; } = "/hubs/inlet";
}